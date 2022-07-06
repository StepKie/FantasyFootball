namespace FantasyFootball.Data;

public static class HistoricalData
{
	public static readonly DateTime EM_2020_START = new(2020, 6, 11, 21, 0, 0);
	public static readonly DateTime WM_2021_START = new(2021, 6, 11, 21, 0, 0);

	public static readonly int[] HISTORIC_WM_YEARS = new[] { 2018, 2022 };
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

	public static IEnumerable<int> AvailableYears(this CompetitionType type)
	{
		return type switch
		{
			CompetitionType.EM => HISTORIC_EM_YEARS,
			CompetitionType.WM => HISTORIC_WM_YEARS,
			_ => Enumerable.Empty<int>(),
		};
	}
}
