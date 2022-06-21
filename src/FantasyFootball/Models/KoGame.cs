namespace FantasyFootball.Models;

/// <summary>
/// Games which may not end in a draw, and usually have a Qualifier from a previous round in a tournament structure
/// </summary>
[Table(nameof(KoGame))]
public class KoGame : Game
{
	[ForeignKey(typeof(Qualifier))]
	public int QualifierId { get; init; }

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<Qualifier> Qualifiers { get; init; }

	[Ignore] public Qualifier HomeQualifier => Qualifiers[0];
	[Ignore] public Qualifier AwayQualifier => Qualifiers[1];

	public KoGame()
	{

	}

	public override Team HomeTeam => HomeQualifier.Get() ?? HomeQualifier.GetStandin();

	public override Team AwayTeam => AwayQualifier.Get() ?? HomeQualifier.GetStandin();

	public KoGame(int idInCompetition, Qualifier qualifierHome, Qualifier qualifierAway, DateTime playedOn)
	{
		PlayedOn = playedOn;
		Qualifiers = new() { qualifierHome, qualifierAway };

	}

	public override void Simulate()
	{
		base.Simulate();

		//TODO Extra time hack
		if (IsKo && HomeScore == AwayScore)
		{
			State = GameState.IN_PROGRESS;
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

}
