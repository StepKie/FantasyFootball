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
		while (!Competition.IsFinished)
		{
			var stage = Competition.Stages.First(stage => !stage.IsFinished);
			await SimulateStage(stage);
		}

		Log.Debug($"Simulation finished");
	}

	public async Task SimulateStage(Stage stage)
	{
		Log.Debug("------------------------------------");
		Log.Debug($"Starting Stage: {stage.Name}");
		Log.Debug("------------------------------------");
		while (!stage.IsFinished)
		{
			await SimulateRound(stage.CurrentRound!);
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
		while (!round.IsFinished)
		{
			await SimulateGame(round.CurrentGame!);
		}
		Log.Debug("--------------------------------------");
	}

	public async Task SimulateGame(Game game)
	{
		if (!game.IsReadyToStart)
		{
			Log.Debug($"Game {game} is not ready to start...");
			return;
		}

		game.Simulate();
		Log.Debug(game.ToString());
		if (!Quiet)
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
