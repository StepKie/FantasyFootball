using System.Text.Json.Serialization;

namespace FantasyFootball.Models;

[Table(nameof(Qualifier))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(GroupQualifier), "group")]
[JsonDerivedType(typeof(GameQualifier), "game")]
public abstract class Qualifier : NamedUniqueId
{
	[OneToOne]
	public KoGame Game { get; set; }

	/// <summary> Can only be resolved after update/insert with children </summary>
	[Ignore]
	public Competition? Competition => Game?.Round?.Stage?.Competition;

	public abstract Team? Get();
	public abstract Team GetPlaceholder();

	/// <summary>
	/// UI-convenience getter that falls back to a placeholder Team when the qualifier
	/// can't yet resolve (group stage in progress, or — rarely — a greedy 3rd-place
	/// allocation that can't fit). <see cref="IgnoreAttribute"/> so it stays out of
	/// the persisted JSON: serializing a getter that can throw on a partially-built
	/// graph would surface as an unhandled exception during Save.
	/// </summary>
	[Ignore]
	public Team QualifiedTeam => Get() ?? GetPlaceholder();

	public static GroupQualifier FromGroup(int groupNo, int place) => new() { GroupId = groupNo, FinalPlacement = place, };
	public static GroupQualifier FromGroup(string letterPlusPlace) => new() { GroupId = "ABCDEFGHIJKL".IndexOf(letterPlusPlace[0]), FinalPlacement = int.Parse(letterPlusPlace.Substring(1, 1)), };
	public static GroupQualifier ThirdPlace(string combination) => new() { FinalPlacement = 3, ThirdPlaceCombination = combination };

	public static GameQualifier FromGame(int gameNo, bool loserQualifies = false) => new() { GameNoInCompetition = gameNo, LoserQualifies = loserQualifies };
}
