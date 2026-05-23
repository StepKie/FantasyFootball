using System.Text.Json.Serialization;

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

	// SQLite back-pointer (inverse of Team.Country). Not part of the persisted shape.
	[OneToMany, JsonIgnore]
	public List<Team> Clubs { get; set; }

	[ForeignKey(typeof(Team))]
	public int NationalTeamId { get; set; }

	// Computed projection — fabricates a Team whose .Country = this. Excluded from JSON to avoid the resulting cycle.
	[OneToOne, JsonIgnore]
	public Team NationalTeam => new() { Country = this, Type = TeamType.NATIONAL_MEN, Name = Name, ShortName = Code3, Elo = Elo };
}
