using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Data.CompetitionFactories;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs /competitions/{id}, the merged Games + Standings page. Loads a
/// Competition by Id, owns the Stage / Round selection + the simulator,
/// and re-publishes change notifications when the simulator fires
/// GameFinishedMessage so the page (which shows games for the current
/// round side-by-side with the standings table) updates as games resolve.
///
/// MAUI splits this into <c>CompetitionDetailViewModel</c> +
/// <c>GamesViewModel</c> + <c>StandingsViewModel</c>; the web port
/// collapses them since both halves render together on one page.
/// </summary>
public partial class CompetitionDetailViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly ISettingsService _settings;
	readonly IDataService _dataService;

	CompetitionSimulator? _simulator;

	// In-memory undo stack of *just-simmed games*. Undo = pop, call game.ClearResult().
	// No snapshots / serialization needed: a simmed game is fully reversible by clearing its score
	// and state back to SCHEDULED. Downstream state (standings, qualifiers, KO bracket) recomputes
	// on the fly from finished games, so nothing else needs to be rewound.
	// Cap at 50: a per-game WC48 run (80 group + KO games) will exceed this and evict the oldest
	// entries beyond the 50 most recent. Acceptable: users rarely undo across many games.
	const int UndoCap = 50;
	readonly Stack<Game> _undoStack = new();

	public CompetitionDetailViewModel(IRepository repo, ISettingsService settings, IDataService dataService)
	{
		_repo = repo;
		_settings = settings;
		_dataService = dataService;

		MessageBus.Register<GameFinishedMessage>(this, (_, msg) => OnGameFinished(msg.FinishedGame));
		MessageBus.Register<DataResetMessage>(this, (_, _) => ClearUndo());
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Stages))]
	[NotifyPropertyChangedFor(nameof(Groups))]
	[NotifyPropertyChangedFor(nameof(Winner))]
	[NotifyPropertyChangedFor(nameof(IsFinished))]
	public partial Competition? Competition { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Rounds))]
	public partial Stage? SelectedStage { get; set; }

	[ObservableProperty]
	public partial Round? SelectedRound { get; set; }

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	/// <summary>
	/// The game whose result was most recently established (or replaced) via a single-game sim.
	/// Set immediately after Simulate / Redo, cleared automatically after ~1.5s so the row's pulse
	/// animation only fires once per action. Multi-game sims (round / stage / tournament) don't pulse.
	/// </summary>
	[ObservableProperty]
	public partial Game? RecentlyFinishedGame { get; set; }

	/// <summary>
	/// Per-session speed override for sim actions. Defaults to the Settings value
	/// on Load; the page's speed control mutates it for the current visit only.
	/// `Instant` short-circuits the inter-game delay and suppresses per-game
	/// re-render messages — a 72-game group stage renders once, not 72 times.
	/// </summary>
	[ObservableProperty]
	public partial SimulationSpeed Speed { get; set; } = SimulationSpeed.Normal;

	public IList<Stage> Stages => Competition?.Stages ?? [];
	public IList<Round> Rounds => SelectedStage?.Rounds ?? [];
	public IList<Group> Groups => Competition?.Groups ?? [];
	public Team? Winner => Competition?.Winner;
	public bool IsFinished => Competition?.IsFinished ?? false;

	public bool CanUndo => _undoStack.Count > 0;

	// The most recently simmed game (top of stack). Drives per-row undo button placement —
	// the button lives on the row of the game it would un-do.
	public Game? UndoTargetGame => _undoStack.TryPeek(out var top) ? top : null;

	public void Load(int competitionId)
	{
		ClearUndo();
		// Clear any in-flight pulse target — a FlashRecentlyFinished from a previous
		// competition would otherwise eventually fire StateHasChanged on this page for nothing.
		RecentlyFinishedGame = null;
		// Replay flow sets IsBusy=true before navigating; clear here so the new comp's view starts idle.
		IsBusy = false;
		Competition = _repo.Get<Competition>(competitionId);
		if (Competition is null) { return; }

		// Sync global type so Back-to-Competitions lands on the same category.
		_dataService.SelectedCompetitionType = Competition.Type;

		SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
		SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();

		// ApplySpeedToSimulator below sets the actual GameDelay from Speed.ToDelay().
		Speed = SimulationSpeedExtensions.FromTimeSpan(_settings.SimulationSpeed);
		_simulator = new CompetitionSimulator(Competition, _repo);
		ApplySpeedToSimulator();
	}

	partial void OnSpeedChanged(SimulationSpeed value) => ApplySpeedToSimulator();

	void ApplySpeedToSimulator()
	{
		if (_simulator is null) { return; }
		_simulator.GameDelay = Speed.ToDelay();
		_simulator.Quiet = Speed == SimulationSpeed.Instant;
	}

	public async Task SimulateGame()
	{
		if (_simulator is null || Competition?.CurrentGame is null || IsBusy) { return; }
		var gameBeingSimmed = Competition.CurrentGame;
		// Set undo target + pulse marker UP FRONT, before the sim's Task.Delay throws an
		// async yield. The next render flushes them in the same frame as the new score —
		// otherwise the buttons + pulse appear ~Task.Delay(GameDelay) ms after the score,
		// visibly lagging the click.
		PushUndoEntry(gameBeingSimmed);
		_ = FlashRecentlyFinished(gameBeingSimmed);
		IsBusy = true;
		try
		{
			// Single-game user click — no inter-game pacing needed; tell the simulator to skip
			// the post-sim Task.Delay so the busy spinner clears immediately after the result.
			await _simulator.SimulateGame(gameBeingSimmed, delayAfter: false);
			_repo.Save(Competition);
		}
		finally
		{
			// In finally, not try, so an exception inside SimulateGame can't leave a stale entry
			// pointing at a still-SCHEDULED game (Undo icon would otherwise appear on an unplayed row).
			if (!gameBeingSimmed.IsFinished) { _undoStack.TryPop(out _); }
			OnSimBatchComplete();
		}
	}

	// Round / Tournament sims do NOT push undo snapshots — undo is scoped to single games.
	// If the user opts into a bigger sim and isn't happy, the recovery path is to re-sim the tournament,
	// not to rewind mass amounts of state. Any prior single-game undo entries are cleared too,
	// since they belong to a graph that's now been simmed past.
	public async Task SimulateRound()
	{
		if (_simulator is null || Competition?.CurrentStage?.CurrentRound is null || IsBusy) { return; }
		ClearUndo();
		IsBusy = true;
		try
		{
			await _simulator.SimulateRound(Competition.CurrentStage.CurrentRound);
			_repo.Save(Competition);
		}
		// Keep the user on the round they just simmed — auto-advancing to the next round hides the results they wanted to see.
		finally { OnSimBatchComplete(advanceSelection: false); }
	}

	public async Task SimulateAll()
	{
		if (_simulator is null || Competition is null || Competition.IsFinished || IsBusy) { return; }
		ClearUndo();
		IsBusy = true;
		try
		{
			await _simulator.Simulate();
			_repo.Save(Competition);
		}
		finally { OnSimBatchComplete(); }
	}

	public void Undo()
	{
		if (Competition is null || IsBusy) { return; }
		if (!_undoStack.TryPop(out var game)) { return; }

		game.ClearResult();
		_repo.Save(Competition);
		// OnSimBatchComplete owns the CanUndo / UndoTargetGame notifications.
		OnSimBatchComplete();
	}

	/// <summary>
	/// Replaces the most recently simmed game's result with a fresh draw. Equivalent to Undo + SimulateGame
	/// on the same game, but in one click. The undo stack is unchanged so the user can still revert this
	/// new result.
	/// </summary>
	public async Task RedoLastGame()
	{
		if (_simulator is null || Competition is null || IsBusy) { return; }
		if (!_undoStack.TryPeek(out var game)) { return; }

		// Pulse fires before the await — same reasoning as SimulateGame, so the new score
		// and the pulse animation land in the same render frame.
		_ = FlashRecentlyFinished(game);
		IsBusy = true;
		try
		{
			game.ClearResult();
			// Same as SimulateGame: single-game user click, no inter-game pacing.
			await _simulator.SimulateGame(game, delayAfter: false);
			_repo.Save(Competition);
		}
		finally
		{
			// Exception-safe pop — if the sim throws after ClearResult, we still leave the stack honest.
			if (!game.IsFinished) { _undoStack.TryPop(out _); }
			OnSimBatchComplete();
		}
	}

	async Task FlashRecentlyFinished(Game game)
	{
		RecentlyFinishedGame = game;
		await Task.Delay(1500);
		// Only clear if no later sim has overwritten us — otherwise the next pulse races with ours.
		if (ReferenceEquals(RecentlyFinishedGame, game)) { RecentlyFinishedGame = null; }
	}

	void PushUndoEntry(Game simmedGame)
	{
		_undoStack.Push(simmedGame);
		// Cap. Stack<T> has no Dequeue, so drop the oldest (bottom-of-stack) by rebuilding from the top.
		// .Take(UndoCap) takes the newest UndoCap entries (Stack enumerates top→bottom), .Reverse()
		// puts them in bottom→top order so pushing back replays the original ordering.
		if (_undoStack.Count > UndoCap)
		{
			var keep = _undoStack.Take(UndoCap).Reverse().ToArray();
			_undoStack.Clear();
			foreach (var g in keep) { _undoStack.Push(g); }
		}
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(UndoTargetGame));
	}

	void ClearUndo()
	{
		if (_undoStack.Count == 0) { return; }
		_undoStack.Clear();
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(UndoTargetGame));
	}

	/// <summary>
	/// Post-sim-batch hook called from every <c>SimulateX</c> finally. Optionally advances
	/// Stage/Round to the current non-finished entry, re-publishes Competition so the page
	/// rebinds, and clears <see cref="IsBusy"/>. SimulateRound passes <c>advanceSelection: false</c>
	/// so the user stays on the round they just simmed; SimulateGame and SimulateAll let the
	/// default advance fire (next-game flow and trophy-landing respectively).
	/// </summary>
	void OnSimBatchComplete(bool advanceSelection = true)
	{
		if (Competition is not null)
		{
			if (advanceSelection)
			{
				SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
				SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();
			}
			OnPropertyChanged(nameof(Competition));
			// Finished competitions are immutable in the UX — the user can't go back to before the trophy.
			if (Competition.IsFinished) { ClearUndo(); }
		}
		IsBusy = false;
		OnPropertyChanged(nameof(CanUndo));
		OnPropertyChanged(nameof(UndoTargetGame));
	}

	/// <summary>
	/// Clones the current competition's team lineup into a fresh competition with the same Type + Year
	/// and saves it. Returns the new Id so the page can navigate to it. Uses today's Elo on each Team
	/// instance, not a snapshot from the finished comp — issue #19 will tighten this once the per-comp
	/// Elo snapshot lands. Group.ShallowClone strips Stage / Games / Id; the factory wires everything else fresh.
	/// </summary>
	public Competition Replay()
	{
		if (Competition is null) { throw new InvalidOperationException("No competition loaded to replay."); }
		var year = Competition.Start?.Year ?? DateTime.Now.Year;
		var clonedGroups = Competition.Groups.Select(g => g.ShallowClone()).ToList();
		var factory = CompetitionFactory.For(Competition.Type, year, clonedGroups);
		var replay = factory.Create();
		_repo.Save(replay);
		MessageBus.Send(new CompetitionCreatedMessage(replay));

		return replay;
	}

	void OnGameFinished(Game finished)
	{
		// Bail if the message is for a different competition.
		if (Competition is null || finished.Round?.Stage?.Competition is null) { return; }
		if (finished.Round.Stage.Competition.Id != Competition.Id) { return; }

		// Re-publish Competition change so groupings + standings + winner re-evaluate. Auto-advance is left
		// to OnSimBatchComplete: doing it per-game during a multi-game sim flipped the panel away from the
		// round being simmed the moment its last game resolved, hiding the results the user was watching.
		OnPropertyChanged(nameof(Competition));
	}
}
