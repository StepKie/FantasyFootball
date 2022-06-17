namespace FantasyFootball.Data;

public class CompetitionSimulator
{
	public TimeSpan GameDelay { get; init; }

	public Competition Competition { get; init; }
	public EuroRoundAdvancer RoundAdvancer { get; init; }

	public IRepository Repo { get; init; }

	public CompetitionSimulator(Competition competition, IRepository repo, int msGameDelay = 100)
	{
		Competition = competition;
		Repo = repo;
		// TODO Select based on competition
		RoundAdvancer = new EuroRoundAdvancer(Competition);
		GameDelay = TimeSpan.FromMilliseconds(msGameDelay);
	}

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
	}

	public async Task SimulateRound(Round round)
	{
		Log.Debug($"Starting Round: {round.Name}");
		Log.Debug("------------------------------------");
		while (!round.IsFinished)
		{
			await SimulateGame(round.CurrentGame!);
		}
	}

	public async Task SimulateGame(Game game)
	{
		if (!game.IsReadyToStart)
		{
			Log.Debug($"Game {game} is not ready to start...");
			return;
		}

		game.Simulate();
		Repo.Save(game);
		Log.Debug(game.ToString());
		RoundAdvancer.CheckAdvanceRound(game);
		await Task.Delay(GameDelay);

		if (Competition.IsFinished)
		{
			Repo.Save(Competition);
		}

		MessagingCenter.Send(game, MessageKeys.GameFinished);
	}

	public static void Print(Group group)
	{
		Log.Debug($"{Res.Group} {group.Name}");
		Log.Debug("------------------------------------");
		Log.Debug($"{"Name",-30} {Res.Games,5}  | {Res.Goals,5}  | {Res.GoalDifference,5}  | {Res.Points,2}");
		foreach (var record in Standings.CreateFrom(group.Stage.Games))
		{
			Log.Debug($"{record.Team.Name,-20} {record.MatchesPlayed,5}  | {record.GoalsFor,2}:{record.GoalsAgainst,2}  | {record.GoalDifference,5}  | {record.Points,5}");
		}
	}
}
