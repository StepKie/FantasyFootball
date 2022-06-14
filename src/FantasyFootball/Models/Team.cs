namespace FantasyFootball.Models;

[Table(nameof(Team))]
public class Team : NamedUniqueId
{
	public TeamType Type { get; init; }
	[Ignore] public bool IsNationalTeam => Type is TeamType.NATIONAL_MEN or TeamType.NATIONAL_WOMEN;
	public virtual string ShortName { get; init; }
	public int Elo { get; set; }
	public virtual string Logo => IconStrings.GetTeamLogo(this);

	[ForeignKey(typeof(Country))]
	public int CountryId { get; set; }

	[ManyToOne(CascadeOperations = CascadeOperation.CascadeRead)]
	public Country Country { get; init; }

	[ForeignKey(typeof(Group))]
	public int GroupId { get; set; }

	public override bool Equals(object? obj) => GetType() == obj?.GetType() && ShortName == (obj as Team)?.ShortName;
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Id, ShortName);
}
