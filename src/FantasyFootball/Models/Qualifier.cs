namespace FantasyFootball.Models;

[Table(nameof(Qualifier))]
public abstract class Qualifier : NamedUniqueId
{
	public Competition Competition { get; set; }

	public abstract Team Get();

	public static GroupQualifer FromGroup(int groupNo, int place) => new() { GroupId = groupNo, FinalPlacement = place, };
	public static GroupQualifer FromGroup(string letterPlusPlace) => new() { GroupId = "ABCDEFGH".IndexOf(letterPlusPlace[0]), FinalPlacement = letterPlusPlace[1], };

	public static GameQualifier FromGame(int gameNo, bool loserQualifies = false) => new() { QualifierGameId = gameNo, LoserQualifies = loserQualifies };
}
