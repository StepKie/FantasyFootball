namespace FantasyFootball.Models;

[Table(nameof(Qualifier))]
public class Qualifier : NamedUniqueId
{
	[OneToOne]
	public KoGame Game { get; set; }

	/// <summary> Can only be resolved after update/insert with children </summary>
	[Ignore]
	public Competition? Competition => Game?.Round?.Stage?.Competition;

	public virtual Team? Get() => null;
	public virtual Team GetStandin() => new();

	public Team GetOrStandIn() => Get() ?? GetStandin();

	public static GroupQualifier FromGroup(int groupNo, int place) => new() { GroupId = groupNo, FinalPlacement = place, };
	public static GroupQualifier FromGroup(string letterPlusPlace) => new() { GroupId = "ABCDEFGH".IndexOf(letterPlusPlace[0]), FinalPlacement = int.Parse(letterPlusPlace.Substring(1, 1)), };
	public static GroupQualifier ThirdPlace(string identifier) => new() { /* TODO */ };

	public static GameQualifier FromGame(int gameNo, bool loserQualifies = false) => new() { GameNoInCompetition = gameNo, LoserQualifies = loserQualifies };
}
