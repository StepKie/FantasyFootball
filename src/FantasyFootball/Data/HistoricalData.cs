namespace FantasyFootball.Data;

public static class HistoricalData
{
	public static readonly DateTime EM_2020_START = new(2020, 6, 11, 21, 0, 0);
	public static readonly DateTime EM_2016_START = new(2020, 6, 10, 21, 0, 0);
	public static readonly DateTime WM_2021_START = new(2021, 6, 11, 21, 0, 0);
	public static readonly DateTime WM_2018_START = new(2018, 6, 14, 17, 0, 0);

	public static readonly int[] HISTORIC_WM_YEARS = new[] { 2018, 2021 };
	public static readonly int[] HISTORIC_EM_YEARS = new[] { 2016, 2020 };

	public static Dictionary<string, string[]> EM_2020_TEAMS => new()
	{
		["A"] = new[] { "ITA", "SUI", "TUR", "WAL" },
		["B"] = new[] { "BEL", "DEN", "FIN", "RUS" },
		["C"] = new[] { "NED", "MKD", "UKR", "AUT" },
		["D"] = new[] { "ENG", "CRO", "SCO", "CZE" },
		["E"] = new[] { "POL", "SWE", "SVK", "ESP" },
		["F"] = new[] { "GER", "FRA", "POR", "HUN" },
	};

	public static Dictionary<string, string[]> EM_2016_TEAMS => new()
	{
		["A"] = new[] { "FRA", "ROU", "ALB", "SUI" },
		["B"] = new[] { "ENG", "RUS", "WAL", "SVK" },
		["C"] = new[] { "GER", "UKR", "POL", "NIR" },
		["D"] = new[] { "ESP", "CZE", "TUR", "CRO" },
		["E"] = new[] { "BEL", "ITA", "IRL", "SWE" },
		["F"] = new[] { "POR", "ISL", "AUT", "HUN" },
	};

	public static Dictionary<string, string[]> WM_2021_TEAMS => new()
	{
		["A"] = new[] { "QAT", "ECU", "SEN", "NED" },
		["B"] = new[] { "ENG", "IRN", "USA", "WAL" },
		["C"] = new[] { "ARG", "KSA", "MEX", "POL" },
		["D"] = new[] { "FRA", "AUS", "DEN", "TUN" },
		["E"] = new[] { "ESP", "CRC", "GER", "JPN" },
		["F"] = new[] { "BEL", "CAN", "MAR", "CRO" },
		["G"] = new[] { "BRA", "SRB", "SUI", "CMR" },
		["H"] = new[] { "POR", "GHA", "URU", "KOR" },
	};

	public static Dictionary<string, string[]> WM_2018_TEAMS => new()
	{
		["A"] = new[] { "RUS", "KSA", "EGY", "URU" },
		["B"] = new[] { "POR", "ESP", "MAR", "IRN" },
		["C"] = new[] { "FRA", "AUS", "PER", "DEN" },
		["D"] = new[] { "ARG", "ISL", "CRO", "NGA" },
		["E"] = new[] { "BRA", "SUI", "CRC", "SRB" },
		["F"] = new[] { "GER", "MEX", "SWE", "KOR" },
		["G"] = new[] { "BEL", "PAN", "TUN", "ENG" },
		["H"] = new[] { "POL", "SEN", "COL", "JPN" },
	};

	public static IEnumerable<int> AvailableYears(this CompetitionType type)
	{
		return type switch
		{
			CompetitionType.EM => HISTORIC_EM_YEARS,
			CompetitionType.WM => HISTORIC_WM_YEARS,
			_ => Enumerable.Empty<int>(),
		};
	}

	public static DateTime StartDate(this CompetitionType type, int year)
	{
		return (type, year) switch
		{
			(CompetitionType.EM, 2016) => EM_2016_START,
			(CompetitionType.EM, 2020) => EM_2020_START,
			(CompetitionType.WM, 2018) => WM_2018_START,
			(CompetitionType.EM, 2021) => WM_2021_START,
			_ => new DateTime(year, 1, 1),
		};
	}
}
