using CommunityToolkit.Mvvm.ComponentModel;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /competitions/{id} page. Loads a Competition by repo Id,
/// owns stage/round selection by Id (strings — Stage/Round are
/// records), and drives the per-game / per-round / full sim through
/// CompetitionSimulator.
/// </summary>
public partial class CompetitionDetailViewModel : ObservableObject
{
	readonly ICompetitionRepository _repo;
	readonly CompetitionSimulator _simulator;
	readonly CompetitionFactory _factory;

	public CompetitionDetailViewModel(
		ICompetitionRepository repo,
		CompetitionSimulator simulator,
		CompetitionFactory factory)
	{
		_repo = repo;
		_simulator = simulator;
		_factory = factory;
	}

	/// <summary>
	/// Game id whose row should play the "just finished" pulse animation.
	/// Set on each single-game sim, cleared 1.5s later (CSS animation length).
	/// Not set by bulk sims (round/stage/all) — pulsing every row would be noisy.
	/// </summary>
	[ObservableProperty]
	public partial int? RecentlyFinishedGameId { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Stages))]
	[NotifyPropertyChangedFor(nameof(IsFinished))]
	[NotifyPropertyChangedFor(nameof(WinnerTeamId))]
	public partial Competition? Competition { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Rounds))]
	public partial string? SelectedStageId { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(RoundGames))]
	[NotifyPropertyChangedFor(nameof(GroupStandings))]
	public partial string? SelectedRoundId { get; set; }

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	public IReadOnlyList<Stage> Stages => Competition?.Stages ?? [];

	public IEnumerable<Round> Rounds => Competition is null
		? []
		: Competition.Rounds.Where(r => r.StageId == SelectedStageId).OrderBy(r => r.Order);

	public IEnumerable<Game> RoundGames => Competition is null || SelectedRoundId is null
		? []
		: Competition.RoundGames(SelectedRoundId).OrderBy(g => g.PlayedOn);

	/// <summary>
	/// Group standings for the selected round. For KO rounds (or when no round is selected),
	/// falls back to all groups so the panel stays populated.
	/// </summary>
	public IReadOnlyList<(string Letter, IReadOnlyList<GroupStanding> Standings)> GroupStandings
	{
		get
		{
			if (Competition is null) { return []; }

			// Group-stage round → only that round's groups; KO round / no round → all groups (panel stays useful).
			var roundLetters = SelectedRoundId is null
				? []
				: Competition.RoundGames(SelectedRoundId)
					.OfType<GroupGame>()
					.Select(g => g.GroupLetter)
					.Distinct()
					.ToList();

			IEnumerable<string> letters = roundLetters.Count > 0
				? roundLetters
				: Enumerable.Range(0, Competition.GroupAssignments.Length).Select(i => ((char)('A' + i)).ToString());

			return letters
				.OrderBy(l => l, StringComparer.Ordinal)
				.Select(l => (Letter: l, Standings: Competition.Standings(l)))
				.ToList();
		}
	}

	public bool IsFinished => Competition?.IsFinished() ?? false;
	public string? WinnerTeamId => Competition?.WinnerTeamId();

	public string? CurrentStageId => CurrentRoundIdInternal() is { } rid
		? Competition?.Rounds.FirstOrDefault(r => r.Id == rid)?.StageId
		: null;

	public string? CurrentRoundId => CurrentRoundIdInternal();

	string? CurrentRoundIdInternal() => Competition?.CurrentGame()?.RoundId;

	public bool IsStageDone(Stage stage) => Competition is not null
		&& Competition.Rounds.Where(r => r.StageId == stage.Id).All(IsRoundDone);

	public bool IsRoundDone(Round round) => Competition is not null
		&& Competition.RoundGames(round.Id).All(g => g.Result is not null);

	public async Task LoadAsync(int competitionId)
	{
		Competition = await _repo.GetAsync(competitionId);
		if (Competition is null) { return; }

		var currentGame = Competition.CurrentGame();
		var currentRoundId = currentGame?.RoundId
			?? Competition.Rounds.OrderByDescending(r => r.Order).FirstOrDefault()?.Id;
		var currentRound = Competition.Rounds.FirstOrDefault(r => r.Id == currentRoundId);
		SelectedStageId = currentRound?.StageId ?? Competition.Stages.LastOrDefault()?.Id;
		SelectedRoundId = currentRoundId;
	}

	partial void OnSelectedStageIdChanged(string? value)
	{
		if (value is null || Competition is null) { SelectedRoundId = null; return; }
		var currentGame = Competition.CurrentGame();
		var currentRound = currentGame is null
			? null
			: Competition.Rounds.FirstOrDefault(r => r.Id == currentGame.RoundId);
		if (currentRound is not null && currentRound.StageId == value)
		{
			SelectedRoundId = currentRound.Id;
		}
		else
		{
			SelectedRoundId = Competition.Rounds
				.Where(r => r.StageId == value)
				.OrderBy(r => r.Order)
				.FirstOrDefault()?.Id;
		}
	}

	public async Task SimulateGame()
	{
		if (Competition is null || IsBusy) { return; }
		var game = Competition.CurrentGame();
		if (game is null) { return; }
		IsBusy = true;
		try
		{
			try
			{
				_simulator.SimulateGame(Competition, game);
				await _repo.SaveAsync(Competition);
			}
			catch
			{
				// Roll back any in-memory mutation so UI + persistence stay consistent on simulator or save failure.
				game.Result = null;
				throw;
			}
			RecentlyFinishedGameId = game.Id;
			_ = ClearPulseAfterDelay(game.Id);
		}
		finally
		{
			RefreshAfterSim();
		}
	}

	/// <summary>
	/// Clear the just-finished pulse marker after the CSS animation duration.
	/// The id-check before clearing avoids racing a later sim — if the user
	/// sims again within 1.5s, the newer id wins.
	/// </summary>
	async Task ClearPulseAfterDelay(int gameId)
	{
		await Task.Delay(1500);
		if (RecentlyFinishedGameId == gameId) { RecentlyFinishedGameId = null; }
	}

	/// <summary>
	/// Undo the most recently played game: clear its Result, clear any KO
	/// auto-fills downstream of it, save. The Undo affordance only ever
	/// targets the chronologically-latest played game, so there's no
	/// played-game-cascade to worry about — but unplayed KO slots that
	/// were auto-resolved on the now-undone result need re-resolving.
	/// </summary>
	public async Task UndoLastGame()
	{
		if (Competition is null || IsBusy) { return; }
		var lastPlayed = Competition.LastFinishedGame();
		if (lastPlayed is null) { return; }

		var originalResult = lastPlayed.Result;
		IsBusy = true;
		try
		{
			try
			{
				lastPlayed.Result = null;
				ClearUnplayedKoResolutions(Competition);
				CompetitionSimulator.ResolveAvailableKoTeams(Competition);
				await _repo.SaveAsync(Competition);
			}
			catch
			{
				// Restore the result; RefreshAfterSim → ResolveAvailableKoTeams re-fills the KO slots from the restored state.
				lastPlayed.Result = originalResult;
				throw;
			}
		}
		finally
		{
			RecentlyFinishedGameId = null;
			RefreshAfterSim(advanceSelection: false);
		}
	}

	/// <summary>
	/// Re-roll the most recently played game: clear its Result, re-score it
	/// with the score model (fresh random outcome), save. Different from Undo
	/// in that the game stays played — just with a new result.
	/// </summary>
	public async Task RerollLastGame()
	{
		if (Competition is null || IsBusy) { return; }
		var lastPlayed = Competition.LastFinishedGame();
		if (lastPlayed is null) { return; }

		var originalResult = lastPlayed.Result;
		IsBusy = true;
		try
		{
			try
			{
				lastPlayed.Result = null;
				ClearUnplayedKoResolutions(Competition);
				_simulator.SimulateGame(Competition, lastPlayed);
				CompetitionSimulator.ResolveAvailableKoTeams(Competition);
				await _repo.SaveAsync(Competition);
			}
			catch
			{
				lastPlayed.Result = originalResult;
				throw;
			}
			RecentlyFinishedGameId = lastPlayed.Id;
			_ = ClearPulseAfterDelay(lastPlayed.Id);
		}
		finally
		{
			RefreshAfterSim(advanceSelection: false);
		}
	}

	static void ClearUnplayedKoResolutions(Competition c)
	{
		foreach (var ko in c.Games.OfType<KoGame>().Where(g => g.Result is null))
		{
			ko.HomeTeamId = null;
			ko.AwayTeamId = null;
		}
	}

	public async Task SimulateRound() => await SimulateRound(SelectedRoundId);

	/// <summary>
	/// Sim every unplayed game chronologically up to and including the
	/// requested round's last game. Honors the "fast-forward to here"
	/// UX of inline play buttons on round chips.
	/// </summary>
	public async Task SimulateRound(string? roundId)
	{
		if (Competition is null || roundId is null || IsFinished || IsBusy) { return; }
		if (Competition.Rounds.All(r => r.Id != roundId)) { return; }

		var roundGames = Competition.RoundGames(roundId).ToList();
		if (roundGames.Count == 0) { return; }

		await SimulateUntil(roundGames.Max(g => g.PlayedOn));
	}

	/// <summary>
	/// Sim every unplayed game chronologically up to and including the
	/// stage's last game. Companion to <see cref="SimulateRound(string?)"/>
	/// for the stage-chip play button.
	/// </summary>
	public async Task SimulateStage(string stageId)
	{
		if (Competition is null || IsFinished || IsBusy) { return; }
		var stageRounds = Competition.Rounds.Where(r => r.StageId == stageId).ToList();
		if (stageRounds.Count == 0) { return; }

		var stageGameIds = stageRounds.SelectMany(r => Competition.RoundGames(r.Id)).Select(g => g.Id).ToHashSet();
		if (stageGameIds.Count == 0) { return; }

		await SimulateUntil(Competition.Games.Where(g => stageGameIds.Contains(g.Id)).Max(g => g.PlayedOn));
	}

	// Shared helper: chronologically sims unplayed games with PlayedOn ≤ cutoff, saves once, rolls back on failure.
	async Task SimulateUntil(DateTime cutoff)
	{
		IsBusy = true;
		var played = new List<Game>();
		try
		{
			try
			{
				// Yield so the IsBusy spinner flushes before the synchronous sim hogs the WASM thread.
				await Task.Yield();
				foreach (var game in Competition!.Games
					.Where(g => g.Result is null && g.PlayedOn <= cutoff)
					.OrderBy(g => g.PlayedOn))
				{
					_simulator.SimulateGame(Competition, game);
					played.Add(game);
				}
				await _repo.SaveAsync(Competition);
			}
			catch
			{
				// Also clear KO ??= stamps from the failed run; otherwise stale upstream-resolver IDs survive into the retry.
				foreach (var g in played) { g.Result = null; }
				ClearUnplayedKoResolutions(Competition!);
				throw;
			}
		}
		finally
		{
			RefreshAfterSim(advanceSelection: false);
		}
	}

	/// <summary>
	/// Clones the loaded competition's lineup into a new scheduled competition.
	/// Returns the new id so the page can navigate. Same Type+Year+teams,
	/// no auto-sim — the new one lands on the detail page ready for play.
	/// </summary>
	public async Task<int?> ReplayAsync()
	{
		if (Competition is null) { return null; }

		var spec = new CustomLineupSpec
		{
			DefinitionId = Competition.DefinitionId,
			Groups = Competition.GroupAssignments.Select(g => (string[])g.Clone()).ToArray(),
		};
		var replay = _factory.Create(spec);
		return await _repo.SaveAsync(replay);
	}

	public async Task SimulateAll()
	{
		if (Competition is null || IsFinished || IsBusy) { return; }
		IsBusy = true;
		try
		{
			// Yield so the IsBusy spinner flushes before the synchronous sim hogs the WASM thread.
			await Task.Yield();
			_simulator.Simulate(Competition);
			await _repo.SaveAsync(Competition);
		}
		finally
		{
			RefreshAfterSim();
		}
	}

	void RefreshAfterSim(bool advanceSelection = true)
	{
		// Fill in resolved KO team ids so flags + names appear without waiting for the user to open those games.
		if (Competition is not null)
		{
			CompetitionSimulator.ResolveAvailableKoTeams(Competition);
		}

		if (Competition is not null && advanceSelection)
		{
			var currentGame = Competition.CurrentGame();
			if (currentGame is not null)
			{
				var currentRound = Competition.Rounds.FirstOrDefault(r => r.Id == currentGame.RoundId);
				if (currentRound is not null)
				{
					if (currentRound.StageId != SelectedStageId) { SelectedStageId = currentRound.StageId; }
					else { SelectedRoundId = currentRound.Id; }
				}
			}
		}

		// Re-publish Competition so all derived properties (Standings, GroupStandings, RoundGames, IsFinished, WinnerTeamId) recompute.
		OnPropertyChanged(nameof(Competition));
		OnPropertyChanged(nameof(RoundGames));
		OnPropertyChanged(nameof(GroupStandings));
		IsBusy = false;
	}
}
