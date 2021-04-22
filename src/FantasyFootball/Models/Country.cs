namespace FantasyFootball.Models;

[Table(nameof(Country))]
public class Country : NamedUniqueId
{
	public string Code2 { get; set; }

	public string Code3 { get; set; }

	public int Elo { get; set; }

	[ForeignKey(typeof(Confederation))]
	public int ConfederationId { get; set; }

	[ManyToOne(CascadeOperations = CascadeOperation.CascadeRead)]
	public Confederation Confederation { get; set; }

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<Team> Clubs { get; set; }

	[ForeignKey(typeof(Team))]
	public int NationalTeamId { get; set; }

	[OneToOne(CascadeOperations = CascadeOperation.All)]
	public Team NationalTeam => new() { Country = this, Type = TeamType.NATIONAL_MEN, Name = Name, ShortName = Code3, Elo = Elo };
}
