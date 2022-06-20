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

	/// <summary>
	/// Same as CompareTo but returns null instead of 0 if both items are equal.
	/// </summary>
	/// <typeparam name="T">IComparable type.</typeparam>
	/// <param name="this">This instance.</param>
	/// <param name="other">The other instance.</param>
	/// <returns>Lexical relation between this and the other instance or null if both are equal.</returns>
	public static int? NullableCompareTo<T>(this T @this, T other) where T : IComparable
	{
		var result = @this.CompareTo(other);
		return result != 0 ? result : null;
	}
}
