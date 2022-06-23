using MathNet.Numerics.Distributions;

namespace FantasyFootball.Models;

/// <summary>
/// TODO There is no good way to create a Game from a string, or create a Game with a result already set, or clear an existing result
/// </summary>
[Table(nameof(Game))]
public class Game : NamedUniqueId
{
	[Ignore]
	public override string Name => $"{Round.Name} {(Round.AllGames.Count > 1 ? Round.AllGames.IndexOf(this) + 1 : "")}";

	public Game()
	{
		// TODO Control access?
	}

	public DateTime PlayedOn { get; init; }

	[ForeignKey(typeof(Team))]
	public int HomeTeamId { get; init; }

	[OneToOne(foreignKey: "HomeTeamId", CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual Team HomeTeam { get; init; }

	[ForeignKey(typeof(Team))]
	public int AwayTeamId { get; init; }

	[OneToOne(foreignKey: "AwayTeamId", CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual Team AwayTeam { get; init; }

	public int HomeScore { get; protected set; }
	public int AwayScore { get; protected set; }

	public GameState State { get; protected set; }

	[Ignore] public bool IsFinished => State == GameState.FINISHED;
	[Ignore] public bool IsNextInRound => Equals(Round?.CurrentGame);
	[Ignore] public bool IsReadyToStart => HomeTeam != null && AwayTeam != null && State == GameState.SCHEDULED;

	[Ignore]
	public string Result => (State, Ending) switch
	{
		(GameState.SCHEDULED, _) => "-:-",
		(GameState.FINISHED, GameEnd.NORMAL) => $"{HomeScore}-{AwayScore}",
		(GameState.FINISHED, GameEnd.EXTRA_TIME) => $"{HomeScore}-{AwayScore} {Res.GameEnd_ExtraTime}",
		(GameState.FINISHED, GameEnd.PENALTIES) => $"{HomeScore}-{AwayScore} {Res.GameEnd_Penalties}",
		_ => $"{HomeScore}-{AwayScore}",
	};

	public GameEnd Ending { get; protected set; }

	[ForeignKey(typeof(Round))]
	public int RoundId { get; set; }

	[ManyToOne]
	public virtual Round Round { get; init; } = new Round { Name = "Not initialized" };

	[Ignore]
	public Team? Winner => (HomeScore > AwayScore) ? HomeTeam : (AwayScore > HomeScore) ? AwayTeam : null;

	[Ignore]
	public Team? Loser => (HomeScore > AwayScore) ? AwayTeam : (AwayScore > HomeScore) ? HomeTeam : null;

	/// <summary> TODO Is it really be the responsibility of Game to "simulate itself"? However, otherwise there would be "feature envy" </summary>
	public virtual void Simulate()
	{
		if (!IsReadyToStart)
		{
			Log.Warning($"Attempted to play uninitialized match -- {this}");
			return;
		}

		State = GameState.IN_PROGRESS;
		Ending = GameEnd.NORMAL;
		HomeScore = 0;
		AwayScore = 0;

		var eloDiff = HomeTeam!.Elo - AwayTeam!.Elo;
		var eloScaleFactor = 0.001 * eloDiff;

		var dist = new Poisson(2.5 + eloScaleFactor);
		var totalGoalsExpected = dist.Sample();

		var expectedScoreElo = 1 / (1 + Math.Pow(10, (HomeTeam.Elo - AwayTeam.Elo) / 400.0));

		while (totalGoalsExpected > 0)
		{
			var rd = new Random().NextDouble();
			if (rd > expectedScoreElo)
			{
				HomeScore++;
			}
			else
			{
				AwayScore++;
			}
			totalGoalsExpected--;
		}

		State = GameState.FINISHED;
	}

	/// <summary> TODO Never used, refactor to use constructor </summary>
	public void SetResult(int homeGoals, int awayGoals, GameEnd end)
	{
		HomeScore = homeGoals;
		AwayScore = awayGoals;
		Ending = end;
		State = GameState.FINISHED;
	}

	public override string ToString() => $"{PlayedOn,-5:g}, {HomeTeam?.ShortName,-2}-{AwayTeam?.ShortName,2} {Result}";
}
