namespace FantasyFootball.Data;

public static class HistoricalData
{

	public static readonly DateTime EM_2024_START = new(2024, 6, 14);
	public static readonly DateTime EM_2020_START = new(2020, 6, 11);
	public static readonly DateTime EM_2016_START = new(2016, 6, 10);
	public static readonly DateTime WM_2022_START = new(2021, 6, 11);
	public static readonly DateTime WM_2018_START = new(2018, 6, 14);

	public static readonly int[] HISTORIC_WM_YEARS = [2018, 2022];
	public static readonly int[] HISTORIC_EM_YEARS = [2016, 2020, 2024];

	public static Dictionary<string, string[]> EM_2024_TEAMS => new()
	{
		["A"] = ["GER", "SCO", "HUN", "SUI"],
		["B"] = ["ESP", "CRO", "ITA", "ALB"],
		["C"] = ["SVN", "DEN", "SRB", "ENG"],
		["D"] = ["POL", "NED", "AUT", "FRA"],
		["E"] = ["BEL", "SVK", "ROU", "UKR"],
		["F"] = ["TUR", "GRE", "POR", "CZE"],
	};

	public static Dictionary<string, string[]> EM_2020_TEAMS => new()
	{
		["A"] = ["TUR", "ITA", "WAL", "SUI"],
		["B"] = ["DEN", "FIN", "BEL", "RUS"],
		["C"] = ["NED", "UKR", "AUT", "MKD"],
		["D"] = ["ENG", "CRO", "SCO", "CZE"],
		["E"] = ["ESP", "SWE", "POL", "SVK"],
		["F"] = ["HUN", "POR", "FRA", "GER"],
	};

	public static Dictionary<string, string[]> EM_2016_TEAMS => new()
	{
		["A"] = ["FRA", "ROU", "ALB", "SUI"],
		["B"] = ["ENG", "RUS", "WAL", "SVK"],
		["C"] = ["GER", "UKR", "POL", "NIR"],
		["D"] = ["ESP", "CZE", "TUR", "CRO"],
		["E"] = ["BEL", "ITA", "IRL", "SWE"],
		["F"] = ["POR", "ISL", "AUT", "HUN"],
	};

	public static Dictionary<string, string[]> WM_2022_TEAMS => new()
	{
		["A"] = ["QAT", "ECU", "SEN", "NED"],
		["B"] = ["ENG", "IRN", "USA", "WAL"],
		["C"] = ["ARG", "KSA", "MEX", "POL"],
		["D"] = ["FRA", "AUS", "DEN", "TUN"],
		["E"] = ["ESP", "CRC", "GER", "JPN"],
		["F"] = ["BEL", "CAN", "MAR", "CRO"],
		["G"] = ["BRA", "SRB", "SUI", "CMR"],
		["H"] = ["POR", "GHA", "URU", "KOR"],
	};

	public static Dictionary<string, string[]> WM_2018_TEAMS => new()
	{
		["A"] = ["RUS", "KSA", "EGY", "URU"],
		["B"] = ["POR", "ESP", "MAR", "IRN"],
		["C"] = ["FRA", "AUS", "PER", "DEN"],
		["D"] = ["ARG", "ISL", "CRO", "NGA"],
		["E"] = ["BRA", "SUI", "CRC", "SRB"],
		["F"] = ["GER", "MEX", "SWE", "KOR"],
		["G"] = ["BEL", "PAN", "TUN", "ENG"],
		["H"] = ["POL", "SEN", "COL", "JPN"],
	};

	public static IEnumerable<int> AvailableYears(this CompetitionType type)
	{
		return type switch
		{
			CompetitionType.EM => HISTORIC_EM_YEARS,
			CompetitionType.WM => HISTORIC_WM_YEARS,
			CompetitionType.CHAMPIONS_LEAGUE => throw new NotImplementedException(),
			CompetitionType.DOMESTIC_LEAGUE => throw new NotImplementedException(),
			_ => Enumerable.Empty<int>(),
		};
	}

	public static DateTime StartDate(this CompetitionType type, int year)
	{
		return (type, year) switch
		{
			(CompetitionType.EM, 2016) => EM_2016_START,
			(CompetitionType.EM, 2020) => EM_2020_START,
			(CompetitionType.EM, 2024) => EM_2016_START,
			(CompetitionType.WM, 2018) => WM_2018_START,
			(CompetitionType.WM, 2022) => WM_2022_START,
			_ => new DateTime(year, 1, 1),
		};
	}
}
