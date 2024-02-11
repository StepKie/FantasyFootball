namespace FantasyFootball.Models;

[Table(nameof(Qualifier))]
public abstract class Qualifier : NamedUniqueId
{
	[OneToOne]
	public KoGame Game { get; set; }

	/// <summary> Can only be resolved after update/insert with children </summary>
	[Ignore]
	public Competition? Competition => Game?.Round?.Stage?.Competition;

	public abstract Team? Get();
	public abstract Team GetPlaceholder();

	public Team QualifiedTeam => Get() ?? GetPlaceholder();

	public static GroupQualifier FromGroup(int groupNo, int place) => new() { GroupId = groupNo, FinalPlacement = place, };
	public static GroupQualifier FromGroup(string letterPlusPlace) => new() { GroupId = "ABCDEFGH".IndexOf(letterPlusPlace[0]), FinalPlacement = int.Parse(letterPlusPlace.Substring(1, 1)), };
	public static GroupQualifier ThirdPlace(string combination) => new() { FinalPlacement = 3, ThirdPlaceCombination = combination };

	public static GameQualifier FromGame(int gameNo, bool loserQualifies = false) => new() { GameNoInCompetition = gameNo, LoserQualifies = loserQualifies };
}
