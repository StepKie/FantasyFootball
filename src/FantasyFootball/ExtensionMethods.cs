namespace FantasyFootball;

public static class ExtensionMethods
{
	/// <summary> Returns a name to display for the CompetitionType</summary>
	public static (string Short, string Long) Name(this CompetitionType competitionType)
	{
		return competitionType switch
		{
			CompetitionType.EM => (Res.EC, Res.European_Championship),
			CompetitionType.WM => (Res.WC, Res.WorldCup),
			CompetitionType.CHAMPIONS_LEAGUE => throw new NotImplementedException(),
			CompetitionType.DOMESTIC_LEAGUE => throw new NotImplementedException(),
			_ => throw new ArgumentException("Unknown Competition Type"),
		};
	}

	/// <summary> Shuffle items</summary>
	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(x => Guid.NewGuid());
}
