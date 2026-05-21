namespace FantasyFootball.Data;

public class CompetitionSimulator(Competition competition, IRepository repo, int msGameDelay = 0)
{
	/// <summary>
	/// Delay between consecutive game simulations. Defaults to 0 (instant) — the
	/// fastest-thing-that-does-the-job. UI callers that want visible pacing
	/// (Slow / Normal / Fast speed picker) opt in explicitly by passing
	/// <paramref name="msGameDelay"/> or by setting this property after construction.
	/// </summary>
	public TimeSpan GameDelay { get; set; } = TimeSpan.FromMilliseconds(msGameDelay);

	/// <summary>
	/// When true, suppresses per-game <see cref="GameFinishedMessage"/> broadcasts
	/// and skips the inter-game <see cref="Task.Delay(TimeSpan)"/>. Used by the
	/// "Instant" speed mode so a 72-game group stage doesn't pay 72 re-renders
	/// (the caller renders once after the whole batch).
	/// </summary>
	public bool Quiet { get; set; }

	public Competition Competition { get; init; } = competition;

	public IRepository Repo { get; init; } = repo;

	public async Task Simulate()
	{
		Stage? lastAttempted = null;
		while (!Competition.IsFinished)
		{
			var stage = Competition.Stages.First(stage => !stage.IsFinished);
			if (ReferenceEquals(stage, lastAttempted))
			{
				Log.Warning($"Tournament sim stuck on stage {stage.Name}; bailing.");
				break;
			}
			lastAttempted = stage;
			await SimulateStage(stage);
		}

		Log.Debug($"Simulation finished");
	}

	public async Task SimulateStage(Stage stage)
	{
		Log.Debug("------------------------------------");
		Log.Debug($"Starting Stage: {stage.Name}");
		Log.Debug("------------------------------------");
		Round? lastAttempted = null;
		while (!stage.IsFinished)
		{
			var current = stage.CurrentRound!;
			if (ReferenceEquals(current, lastAttempted))
			{
				Log.Warning($"Stage {stage.Name} stuck on round {current.Name}; bailing.");
				break;
			}
			lastAttempted = current;
			await SimulateRound(current);
			foreach (var group in stage.Groups)
			{
				Print(group);
			}
		}
		Log.Debug("------------------------------------");
	}

	public async Task SimulateRound(Round round)
	{
		Log.Debug("--------------------------------------");
		Log.Debug($"Starting Round: {round.Name}");
		Log.Debug("--------------------------------------");
		// Iterate by PlayedOn so non-Instant sims fill rows in the same order the UI displays them
		// (CompetitionDetail orders games by PlayedOn). Round.AllGames is factory-defined order
		// (group A first, then B, …) which interleaves with chronological kickoff times — without
		// this snapshot, row 3 (16:00) would update *after* row 4 (15:00) once we work down each group.
		var ordered = round.AllGames.OrderBy(g => g.PlayedOn).ToList();
		foreach (var game in ordered)
		{
			if (game.IsFinished) { continue; }
			// Skip rather than break: a placeholder-team KO game (unresolved qualifier)
			// shouldn't sim, but also shouldn't block later games in the same round.
			if (!game.IsReadyToStart)
			{
				Log.Warning($"Round {round.Name}: game {game} is not ready; skipping.");
				continue;
			}
			await SimulateGame(game);
		}
		Log.Debug("--------------------------------------");
	}

	/// <summary>
	/// Simulates a single game. When called from a multi-game loop (Round / Stage / Tournament)
	/// the caller leaves <paramref name="delayAfter"/> as true so the GameDelay pacing applies
	/// between games. For a one-off user click on Sim Game / Redo there is no "next game" to
	/// pace against — the delay would just block the busy spinner from clearing — so the VM
	/// passes false.
	/// </summary>
	public async Task SimulateGame(Game game, bool delayAfter = true)
	{
		if (!game.IsReadyToStart)
		{
			Log.Debug($"Game {game} is not ready to start...");
			// Yield even on early-return; defence in depth in case any caller spins on a non-ready game.
			await Task.Yield();
			return;
		}

		game.Simulate();
		Log.Debug(game.ToString());
		if (Quiet)
		{
			// Yield without delay so the browser can render the busy spinner and stay
			// responsive even on a 48-team tournament. Skipping this turns the entire
			// sim into one synchronous chunk and the page appears frozen.
			await Task.Yield();
		}
		else
		{
			MessageBus.Send(new GameFinishedMessage(game));
			if (delayAfter) { await Task.Delay(GameDelay); }
		}

		// Persistence is the caller's responsibility — saving per game escalates to a full
		// Competition-graph write on LocalStorage (Game isn't an aggregate root, so it bubbles
		// up to Save<Competition>), which was the main bottleneck during group-stage sim.
		// Callers save once per sim action (Game / Round / Stage / Tournament).

		if (Competition.IsFinished)
		{
			MessageBus.Send(new CompetitionFinishedMessage(Competition));
		}
	}

	public static void Print(Group group)
	{
		Log.Debug($"{Res.Group} {group.Name}");
		Log.Debug("------------------------------------");
		Log.Debug($"{"Name",-30} {Res.Games,5}  | {Res.Goals,5}  | {Res.GoalDifference,5}  | {Res.Points,2}");
		foreach (var record in group.GetStandings())
		{
			Log.Debug($"{record.Team.Name,-20} {record.MatchesPlayed,5}  | {record.GoalsFor,2}:{record.GoalsAgainst,2}  | {record.GoalDifference,5}  | {record.Points,5}");
		}
	}
}
