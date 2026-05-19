namespace FantasyFootball.Data;

public class CompetitionSimulator(Competition competition, IRepository repo, int msGameDelay = 100)
{
	/// <summary>
	/// Delay between consecutive game simulations. Mutable so the UI's per-session
	/// speed control (Slow / Normal / Fast / Instant) can override the Settings
	/// default without rebuilding the simulator.
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
		Game? lastAttempted = null;
		while (!round.IsFinished)
		{
			var current = round.CurrentGame!;
			// Progress check: if SimulateGame can't advance the same game twice in a row,
			// the round is stuck (typically a KoGame whose qualifier returned a placeholder
			// because greedy 3rd-place allocation failed — issue #12). Bail rather than
			// spinning forever and freezing the browser tab.
			if (ReferenceEquals(current, lastAttempted))
			{
				Log.Warning($"Round {round.Name}: game {current} stays non-ready; bailing out of sim loop.");
				break;
			}
			lastAttempted = current;
			await SimulateGame(current);
		}
		Log.Debug("--------------------------------------");
	}

	public async Task SimulateGame(Game game)
	{
		if (!game.IsReadyToStart)
		{
			Log.Debug($"Game {game} is not ready to start...");
			// Yield even on the early-return path so the browser stays responsive
			// if any other caller spins on us — defence in depth against the freeze
			// SimulateRound's progress-check now prevents.
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
			await Task.Delay(GameDelay);
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
