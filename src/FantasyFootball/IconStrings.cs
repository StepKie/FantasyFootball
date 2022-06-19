namespace FantasyFootball;

public class IconStrings
{
	//TODO Return (from folder?) based on country code. Older versions (from web)
	// $"https://flagcdn.com/h80/{country.Code2.ToLower()}.png"
	// $"https://hatscripts.github.io/circle-flags/flags/{country.Code2.ToLower()}.svg"
	public static string GetNationalFlagWeb(Country country) => $"{country.Name.ToLower().Replace(" ", "_").Replace("-", "_").Replace("'", "_")}.png";
	public static string GetConfederationLogo(Confederation confederation) => "";

	public static string? GetCompetitionLogo(CompetitionType competitionType) =>
		competitionType switch
		{
			CompetitionType.EM => "logo_uefa",
			CompetitionType.WM => "world",
			CompetitionType.CHAMPIONS_LEAGUE => "logo_uefa_cl",
			CompetitionType.DOMESTIC_LEAGUE => null,
			_ => null,
		};

	// TODO Return (from folder?) based on club id
	public static string GetTeamLogo(Team team) => team.IsNationalTeam ? GetNationalFlagWeb(team.Country) : "question_mark";
}
