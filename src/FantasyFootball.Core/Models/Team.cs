using System.Text.Json.Serialization;

namespace FantasyFootball.Models;

public class Team : NamedUniqueId
{
	public TeamType Type { get; init; }
	[JsonIgnore] public bool IsNationalTeam => Type is TeamType.NATIONAL_MEN or TeamType.NATIONAL_WOMEN;
	public virtual string ShortName { get; init; }

	/// <summary>Compact display name for narrow layouts ("Gladbach", "Man City"). Null for national teams, whose names compact via hyphenation instead.</summary>
	public string? CompactName { get; init; }
	[JsonIgnore] public virtual string Logo => IconStrings.GetTeamLogo(this);

	public int CountryId { get; set; }

	public Country Country { get; init; }

	public override bool Equals(object? obj) => GetType() == obj?.GetType() && ShortName == (obj as Team)?.ShortName;
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Id, ShortName);
}
