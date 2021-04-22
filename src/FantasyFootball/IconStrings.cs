namespace FantasyFootball;

public class IconStrings
{
	//TODO Return (from folder?) based on country code
	public static string GetNationalFlagWeb(Country country) => $"https://flagcdn.com/h80/{country.Code2.ToLower()}.png";
	public static string GetConfederationLogo(Confederation confederation) => "";

	public static string? GetCompetitionLogo(CompetitionType competitionType) =>
		competitionType switch
		{
			CompetitionType.EM => "logo_uefa",
			CompetitionType.WM or CompetitionType.CHAMPIONS_LEAGUE or CompetitionType.DOMESTIC_LEAGUE => null,
			_ => null,
		};

	// TODO Return (from folder?) based on club id
	public static string GetTeamLogo(Team team) => team.IsNationalTeam ? GetNationalFlagWeb(team.Country) : "";
}
