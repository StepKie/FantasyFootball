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
			CompetitionType.CHAMPIONS_LEAGUE => ("UCL", "UEFA Champions League"),
			CompetitionType.DOMESTIC_LEAGUE => ("League", "Domestic League"),
			_ => throw new ArgumentException("Unknown Competition Type"),
		};
	}

	/// <summary>
	/// Slug part of a competition's <see cref="Models.Competition.DefinitionId"/> — everything before
	/// the first digit, trimmed of trailing dashes. E.g. <c>"bundesliga-2025-2026"</c> → <c>"bundesliga"</c>,
	/// <c>"premier-league-2003-2004"</c> → <c>"premier-league"</c>, <c>"wm-2022"</c> → <c>"wm"</c>.
	/// Used by the UI to pick a per-league logo for DOMESTIC_LEAGUE competitions where
	/// <c>CompetitionType</c> alone isn't enough to disambiguate.
	/// </summary>
	public static string LeagueKey(this Models.Competition competition)
	{
		var id = competition.DefinitionId;
		var firstDigit = id.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
		return firstDigit <= 0 ? id : id[..firstDigit].TrimEnd('-');
	}

	/// <summary> Shuffle items</summary>
	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(x => Guid.NewGuid());

	public static void Replace<T>(this List<T> list, Predicate<T> oldItemSelector, T newItem)
	{
		var oldItemIndex = list.FindIndex(oldItemSelector);
		list[oldItemIndex] = newItem;
	}

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
