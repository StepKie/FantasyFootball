using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
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

	// In-memory rewind stack. JSON snapshot per user-initiated sim action (Game / Round / Stage / Tournament).
	// Cap mirrors the failure-log sketch: 50 is more than a per-game WC48 run would ever push.
	const int UndoCap = 50;
	readonly Stack<string> _undoStack = new();

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

	public void Load(int competitionId)
	{
		ClearUndo();
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
		PushUndoSnapshot();
		IsBusy = true;
		try
		{
			await _simulator.SimulateGame(Competition.CurrentGame);
			_repo.Save(Competition);
		}
		finally { OnSimBatchComplete(); }
	}

	// Round / Stage / Tournament sims do NOT push undo snapshots — undo is scoped to single games.
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
		finally { OnSimBatchComplete(); }
	}

	public async Task SimulateStage()
	{
		if (_simulator is null || Competition?.CurrentStage is null || IsBusy) { return; }
		ClearUndo();
		IsBusy = true;
		try
		{
			await _simulator.SimulateStage(Competition.CurrentStage);
			_repo.Save(Competition);
		}
		finally { OnSimBatchComplete(); }
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
		if (_undoStack.Count == 0 || Competition is null || IsBusy) { return; }
		var json = _undoStack.Pop();
		var restored = CompetitionSnapshot.Deserialize(json);
		if (restored is null) { OnPropertyChanged(nameof(CanUndo)); return; }

		Competition = restored;
		_repo.Save(Competition);
		_simulator = new CompetitionSimulator(Competition, _repo);
		ApplySpeedToSimulator();
		// Re-resolve selection against the new object graph; OnSimBatchComplete does exactly that.
		OnSimBatchComplete();
		OnPropertyChanged(nameof(CanUndo));
	}

	void PushUndoSnapshot()
	{
		if (Competition is null) { return; }
		_undoStack.Push(CompetitionSnapshot.Serialize(Competition));
		// Cap. Stack<T> has no Dequeue, so drop oldest by rebuilding when over.
		if (_undoStack.Count > UndoCap)
		{
			var keep = _undoStack.Take(UndoCap).Reverse().ToArray();
			_undoStack.Clear();
			foreach (var s in keep) { _undoStack.Push(s); }
		}
		OnPropertyChanged(nameof(CanUndo));
	}

	void ClearUndo()
	{
		if (_undoStack.Count == 0) { return; }
		_undoStack.Clear();
		OnPropertyChanged(nameof(CanUndo));
	}

	/// <summary>
	/// Post-sim-batch hook called from every <c>SimulateX</c> finally. Advances
	/// Stage/Round to the current non-finished entry, re-publishes Competition
	/// so the page rebinds, and clears <see cref="IsBusy"/>. OnGameFinished
	/// keeps selection live in non-Quiet mode per game, but Quiet/Instant
	/// suppresses those broadcasts — without this hook the page would still be
	/// pinned to the round selected before the batch started.
	/// </summary>
	void OnSimBatchComplete()
	{
		if (Competition is not null)
		{
			SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
			SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();
			OnPropertyChanged(nameof(Competition));
			// Finished competitions are immutable in the UX — the user can't go back to before the trophy.
			if (Competition.IsFinished) { ClearUndo(); }
		}
		IsBusy = false;
		OnPropertyChanged(nameof(CanUndo));
	}

	void OnGameFinished(Game finished)
	{
		// Bail if the message is for a different competition.
		if (Competition is null || finished.Round?.Stage?.Competition is null) { return; }
		if (finished.Round.Stage.Competition.Id != Competition.Id) { return; }

		// Auto-advance Stage/Round to the next non-finished one so the games
		// pane follows the simulation forward.
		SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
		SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();

		// Re-publish Competition change so groupings + standings + winner re-evaluate.
		OnPropertyChanged(nameof(Competition));
	}
}
