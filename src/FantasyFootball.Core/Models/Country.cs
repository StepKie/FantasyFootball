using System.Text.Json.Serialization;

namespace FantasyFootball.Models;

public class Country : NamedUniqueId
{
	public string Code2 { get; set; }

	public string Code3 { get; set; }

	public int ConfederationId { get; set; }

	public Confederation Confederation { get; set; }

	// Computed projection — fabricates a Team whose .Country = this. Excluded from JSON to avoid the resulting cycle.
	[JsonIgnore]
	public Team NationalTeam => new() { Country = this, Type = TeamType.NATIONAL_MEN, Name = Name, ShortName = Code3 };
}
