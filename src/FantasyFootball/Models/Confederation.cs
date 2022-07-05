namespace FantasyFootball.Models;

[Table(nameof(Confederation))]
public class Confederation : NamedUniqueId
{
	public static List<Confederation> ALL => new() { UEFA, CAF, CONMEBOL, CONCACAF, OFC, AFC };

	public static readonly Confederation UEFA = new(nameof(UEFA), "Europe", 14);
	public static readonly Confederation CAF = new(nameof(CAF), "Africa", 5);
	public static readonly Confederation CONMEBOL = new(nameof(CONMEBOL), "South America", 5);
	public static readonly Confederation CONCACAF = new(nameof(CONCACAF), "North and Middle America", 3);
	public static readonly Confederation OFC = new(nameof(OFC), "Australia and Oceania", 1);
	public static readonly Confederation AFC = new(nameof(AFC), "Asia", 4);

	public static readonly Confederation UNKNOWN = new("?", "?", 0);

	public Confederation() { /* Ignore: public parameterless constructor for SQLite, DO NOT USE */ }

	/// <summary> Do not set Id manually, rather let SQLite assign the id! </summary>
	Confederation(string name, string continent, int noOfWmPartipants)
	{
		Name = name;
		Continent = continent;
		NoOfWmParticipants = noOfWmPartipants;
	}

	public string Continent { get; init; }

	[OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual List<Country> Countries { get; init; } = new();

	public string Logo => IconStrings.GetConfederationLogo(this);
	public int NoOfWmParticipants { get; init; }

	public override bool Equals(object? obj) => GetType() == obj?.GetType() && Name == (obj as Confederation)?.Name;

	public override int GetHashCode() => base.GetHashCode();

	public static Confederation FromName(string name) => ALL.FirstOrDefault(c => c.Name == name) ?? UNKNOWN;
}
