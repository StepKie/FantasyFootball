using MathNet.Numerics.Distributions;

namespace FantasyFootball.Models;

/// <summary>
/// TODO There is no good way to create a Game from a string, or create a Game with a result already set, or clear an existing result
/// </summary>
[Table(nameof(Game))]
public class Game : NamedUniqueId
{

	public override string Name => $"{Round.Name} {Round.Stage.Games.IndexOf(this)}";

	public Game()
	{
		// TODO Control access?
	}

	public Game(int idInCompetition, Qualifier qualifierHome, Qualifier qualifierAway, DateTime playedOn)
	{
		IsKo = true;
		PlayedOn = playedOn;
	}

	public DateTime PlayedOn { get; init; }

	[ForeignKey(typeof(Team))]
	public int HomeTeamId { get; set; }

	[OneToOne(foreignKey: "HomeTeamId", CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual Team? HomeTeam { get; set; }

	public string HomeTeamTentative { get; init; } = "TBD";

	[ForeignKey(typeof(Team))]
	public int AwayTeamId { get; set; }

	[OneToOne(foreignKey: "AwayTeamId", CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual Team? AwayTeam { get; set; }

	public string AwayTeamTentative { get; init; } = "TBD";

	public int HomeScore { get; set; }
	public int AwayScore { get; set; }

	public bool IsKo { get; init; }

	public GameState State { get; set; }

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

	public GameEnd Ending { get; private set; }

	[ForeignKey(typeof(Round))]
	public int RoundId { get; set; }

	[ManyToOne]
	public Round Round { get; set; }

	[Ignore]
	public Team? Winner => (HomeScore > AwayScore) ? HomeTeam : (AwayScore > HomeScore) ? AwayTeam : null;

	[Ignore]
	public Team? Loser => (HomeScore > AwayScore) ? AwayTeam : (AwayScore > HomeScore) ? HomeTeam : null;

	/// <summary> TODO Is it really be the responsibility of Game to "simulate itself"? However, otherwise there would be "feature envy" </summary>
	public void Simulate()
	{
		if (!IsReadyToStart)
		{
			Log.Warning($"Attempted to play uninitialized match -- {this}");
			return;
		}

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
		//TODO Extra time hack
		if (IsKo && HomeScore == AwayScore)
		{
			Ending = GameEnd.EXTRA_TIME;
			var rd = new Random().NextDouble();
			if (rd < 0.1)
			{
				HomeScore += 2;
			}
			else if (rd < 0.5)
			{
				HomeScore += 1;
			}
			else if (rd < 0.9)
			{
				AwayScore += 1;
			}
			else
			{
				AwayScore += 2;
			}
		}

		State = GameState.FINISHED;
	}

	public void AddParticipant(Team participant)
	{
		if (IsReadyToStart) { throw new ArgumentException($"Can't add {participant} to finalized game {this}"); }

		if (HomeTeam == null)
		{
			HomeTeam = participant;
		}
		else
		{
			AwayTeam = participant;
		}
	}

	public void AddParticipants(Team? home, Team? away)
	{
		// TODO Seems fishy (both are checked for not null?!)
		if (HomeTeam != null || AwayTeam != null) { throw new ArgumentException($"Can't add {home} and {away} to game {this} with at least one participant set"); }

		HomeTeam = home;
		AwayTeam = away;
	}

	/// <summary> TODO Never used, refactor to use constructor </summary>
	public void SetResult(int homeGoals, int awayGoals, GameEnd end)
	{
		HomeScore = homeGoals;
		AwayScore = awayGoals;
		Ending = end;
		State = GameState.FINISHED;
	}

	public override string ToString() => $"{PlayedOn,-5:g}, {HomeTeam?.ShortName ?? HomeTeamTentative,-2} {Result} {AwayTeam?.ShortName ?? AwayTeamTentative,2}";
}
