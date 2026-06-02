namespace FantasyFootball;

public class IconStrings
{
	//TODO Return (from folder?) based on country code. Older versions (from web)
	// $"https://flagcdn.com/h80/{country.Code2.ToLower()}.png"
	// $"https://hatscripts.github.io/circle-flags/flags/{country.Code2.ToLower()}.svg"
	public static string GetNationalFlagWeb(Country country) => $"{country.Name.ToLower().Replace(" ", "_").Replace("-", "_").Replace("'", "_")}.png";

	/// <summary> TODO Add confederation logos if they are found </summary>
	public static string GetConfederationLogo(Confederation confederation) => $"{confederation.Name.ToLower()}.png";

	public static string? GetCompetitionLogo(CompetitionType competitionType) =>
		competitionType switch
		{
			CompetitionType.EM => "logo_uefa",
			CompetitionType.WM => "world",
			CompetitionType.CHAMPIONS_LEAGUE => "logo_uefa_cl",
			_ => null,
		};

	public static string GetTeamLogo(Team team) => team.Type switch
	{
		TeamType.NATIONAL_MEN or TeamType.NATIONAL_WOMEN => GetNationalFlagWeb(team.Country),
		TeamType.CLUB_MEN or TeamType.CLUB_WOMEN => $"clubs/{team.ShortName.ToLowerInvariant()}.png",
		_ => "question_mark",
	};
}
