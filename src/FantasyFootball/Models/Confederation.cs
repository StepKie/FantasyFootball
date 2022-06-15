namespace FantasyFootball.Models;

[Table(nameof(Confederation))]
public class Confederation : NamedUniqueId
{
	public static List<Confederation> ALL => new() { UEFA, CAF, CONMEBOL, CONCACAF, OFC, AFC };

	public static readonly Confederation UEFA = new(1, nameof(UEFA), "Europe", 14);
	public static readonly Confederation CAF = new(2, nameof(CAF), "Africa", 5);
	public static readonly Confederation CONMEBOL = new(3, nameof(CONMEBOL), "South America", 5);
	public static readonly Confederation CONCACAF = new(4, nameof(CONCACAF), "North and Middle America", 3);
	public static readonly Confederation OFC = new(5, nameof(OFC), "Australia and Oceania", 1);
	public static readonly Confederation AFC = new(6, nameof(AFC), "Asia", 4);

	public static readonly Confederation UNKNOWN = new(7, "?", "?", 0);

	public Confederation(int id, string name, string continent, int noOfWmPartipants)
	{
		Id = id;
		Name = name;
		Continent = continent;
		NoOfWmParticipants = noOfWmPartipants;
	}

	public string Continent { get; init; }

	[OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual List<Country> Countries { get; init; } = new();

	public string Logo => IconStrings.GetConfederationLogo(this);
	public int NoOfWmParticipants { get; init; }

	public static Confederation FromName(string name) => ALL.FirstOrDefault(c => c.Name == name) ?? UNKNOWN;
}
