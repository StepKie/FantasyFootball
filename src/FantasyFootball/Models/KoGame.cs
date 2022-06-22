namespace FantasyFootball.Models;

/// <summary>
/// Games which may not end in a draw, and usually have a Qualifier from a previous round in a tournament structure
/// </summary>
[Table(nameof(KoGame))]
public class KoGame : Game
{
	#region Qualifiers

	// Collapse, ugly...
	// It is implemented like that because we can not simply add Qualifier base class in SQLite, during retrieval this will not find the correct subclass since it is in a different table

	[ForeignKey(typeof(GroupQualifier))] public int HomeGroupQualifierId { get; init; }
	[ForeignKey(typeof(GroupQualifier))] public int AwayGroupQualifierId { get; init; }
	[ForeignKey(typeof(GameQualifier))] public int HomeGameQualifierId { get; init; }
	[ForeignKey(typeof(GameQualifier))] public int AwayGameQualifierId { get; init; }

	[OneToOne(foreignKey: "HomeGroupQualifierId", CascadeOperations = CascadeOperation.All)] public GroupQualifier? HomeGroupQualifier { get; init; }
	[OneToOne(foreignKey: "AwayGroupQualifierId", CascadeOperations = CascadeOperation.All)] public GroupQualifier? AwayGroupQualifier { get; init; }
	[OneToOne(foreignKey: "HomeGameQualifierId", CascadeOperations = CascadeOperation.All)] public GameQualifier? HomeGameQualifier { get; init; }
	[OneToOne(foreignKey: "AwayGameQualifierId", CascadeOperations = CascadeOperation.All)] public GameQualifier? AwayGameQualifier { get; init; }

	[Ignore] public Qualifier HomeQualifier => HomeGroupQualifier as Qualifier ?? HomeGameQualifier!;
	[Ignore] public Qualifier AwayQualifier => AwayGroupQualifier as Qualifier ?? AwayGameQualifier!;

	#endregion

	public KoGame()
	{
		// Needed for SQLite
	}

	public override Team HomeTeam => HomeQualifier.Get() ?? HomeQualifier.GetStandin();

	public override Team AwayTeam => AwayQualifier.Get() ?? AwayQualifier.GetStandin();

	public KoGame(int idInCompetition, Qualifier qualifierHome, Qualifier qualifierAway, DateTime playedOn)
	{
		PlayedOn = playedOn;
		HomeGroupQualifier = qualifierHome as GroupQualifier;
		HomeGameQualifier = qualifierHome as GameQualifier;
		AwayGroupQualifier = qualifierAway as GroupQualifier;
		AwayGameQualifier = qualifierAway as GameQualifier;

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
