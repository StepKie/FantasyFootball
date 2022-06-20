namespace FantasyFootball.Models;

[Table(nameof(Qualifier))]
public abstract class Qualifier : NamedUniqueId
{

	[ForeignKey(typeof(KoGame))]
	public int GameId { get; init; }

	[OneToOne]
	public KoGame Game { get; set; }

	/// <summary> Can only be resolved after update/insert with children </summary>
	[Ignore]
	public Competition? Competition => Game?.Round?.Stage?.Competition;

	public abstract Team? Get();
	public abstract Team GetStandin();

	public static GroupQualifer FromGroup(int groupNo, int place) => new() { GroupId = groupNo, FinalPlacement = place, };
	public static GroupQualifer FromGroup(string letterPlusPlace) => new() { GroupId = "ABCDEFGH".IndexOf(letterPlusPlace[0]), FinalPlacement = letterPlusPlace[1], };
	public static GroupQualifer ThirdPlace(string identifier) => new() { /* TODO */ };

	public static GameQualifier FromGame(int gameNo, bool loserQualifies = false) => new() { QualifierGameId = gameNo, LoserQualifies = loserQualifies };
}
