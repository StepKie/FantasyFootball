namespace FantasyFootball.Models;

[Table(nameof(Confederation))]
public class Confederation : NamedUniqueId
{
	public static List<Confederation> All => new() { UEFA, CAF, CONMEBOL, CONCACAF, OFC, AFC };
	public string Continent { get; init; }

	[OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
	public virtual List<Country> Countries { get; init; } = new();
	public string Logo => IconStrings.GetConfederationLogo(this);
	public int NoOfWmParticipants { get; init; }

	public static Confederation UEFA = new("UEFA", "Europe", 14);
	public static Confederation CAF = new("CAF", "Africa", 5);
	public static Confederation CONMEBOL = new("CONMEBOL", "South America", 5);
	public static Confederation CONCACAF = new("CONCACAF", "North and Middle America", 3);
	public static Confederation OFC = new("OFC", "Australia and Oceania", 1);
	public static Confederation AFC = new("AFC", "Asia", 4);

	public static Confederation UNKNOWN = new("?", "?", 0);

	public Confederation() { }

	public Confederation(string name, string continent, int noOfWmPartipants)
	{
		Name = name;
		Continent = continent;
		NoOfWmParticipants = noOfWmPartipants;
	}

	public static Confederation FromName(string name) => All.FirstOrDefault(c => c.Name == name) ?? UNKNOWN;

}
