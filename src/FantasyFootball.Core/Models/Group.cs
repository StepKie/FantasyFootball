namespace FantasyFootball.Models;

[Table(nameof(Group))]
public class Group : NamedUniqueId
{
	[ForeignKey(typeof(Stage))]
	public int StageId { get; set; }

	[ManyToOne]
	public Stage Stage { get; set; }

	//TODO May be ManyToMany (i.e. a Team can be in a Cup and a National League ...)
	[ManyToMany(typeof(TeamGroupAssignment), CascadeOperations = CascadeOperation.CascadeRead)]
	public List<Team> Teams { get; set; } = [];

	public IList<TeamRecord> GetStandings() => Standings.CreateFrom(Games);

	[Ignore]
	public IList<Game> Games => Stage.Games.Where(g => Teams.Contains(g.HomeTeam!) && Teams.Contains(g.AwayTeam!)).ToList();

	[Ignore] public bool IsFinished => Games.All(g => g.IsFinished);

	/// <summary>
	/// Copies all factory-settable properties to a new Group instance.
	/// <see cref="StageId"/>, <see cref="Stage"/>, and <see cref="NamedUniqueId.Id"/>
	/// are intentionally NOT copied — they're set post-construction by SQLite
	/// (FK hydration) or by <c>CompetitionFactory.WireBackReferences</c>.
	/// **Update this method whenever Group gains a new factory-settable property.**
	/// </summary>
	public Group ShallowClone() => new()
	{
		Name = Name,
		Teams = [.. Teams],
	};
}
