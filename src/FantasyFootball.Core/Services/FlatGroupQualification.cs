using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Per-group qualification status for the standings panel: which group
/// positions advance directly to the next stage (via group-placement
/// qualifiers like <c>A1</c>) and whether the group's 3rd-placer is
/// in any 3rd-place pool — and if so, whether they actually advanced.
///
/// Pure projection over a <see cref="FlatCompetition"/>: walks every
/// <see cref="FlatKoGame"/>'s qualifier DSL to discover the topology,
/// then consults the current standings to fill in the "advanced?"
/// answer once the group stage has finished.
/// </summary>
public static class FlatGroupQualification
{
	/// <summary>
	/// Qualification slots for a single group.
	/// </summary>
	/// <param name="DirectSlots">
	/// Positions referenced by direct group-placement qualifiers (e.g. position 1 for an <c>A1</c> ref).
	/// </param>
	/// <param name="ThirdPlaceEligible">
	/// True if this group's letter appears in any <see cref="FlatThirdPlacePool"/>.
	/// </param>
	/// <param name="ThirdPlaceAdvanced">
	/// True if the group's 3rd-placer actually made the pool cut; false if they
	/// didn't; null if the pool hasn't resolved yet (group stage incomplete).
	/// Always null when <see cref="ThirdPlaceEligible"/> is false.
	/// </param>
	public readonly record struct GroupQualSlots(
		IReadOnlySet<int> DirectSlots,
		bool ThirdPlaceEligible,
		bool? ThirdPlaceAdvanced);

	/// <summary>
	/// Computes a per-group-letter qualification map for the entire competition.
	/// Returns an empty dictionary if <paramref name="c"/> is null.
	/// </summary>
	public static Dictionary<string, GroupQualSlots> Compute(FlatCompetition? c)
	{
		var map = new Dictionary<string, GroupQualSlots>();
		if (c is null) { return map; }

		var directByGroup = new Dictionary<string, HashSet<int>>();
		var thirdEligible = new HashSet<string>();

		foreach (var ko in c.Games.OfType<FlatKoGame>())
		{
			AddQualifier(ko.HomeQual, directByGroup, thirdEligible);
			AddQualifier(ko.AwayQual, directByGroup, thirdEligible);
		}

		// 3rd-place pool resolution: the pool can't resolve until EVERY group's games finish
		// (cross-group rank comparison). Once it does, ResolveAvailableKoTeams fills the KO
		// slot's HomeTeamId/AwayTeamId. If that resolved id matches THIS group's 3rd-place
		// team → advanced. If the pool has resolved and our 3rd-placer isn't in any of those
		// slots → out. Until then → undetermined (null).
		var allGroupGamesPlayed = c.Games.OfType<FlatGroupGame>().All(g => g.Result is not null);
		var groupLetters = directByGroup.Keys.Concat(thirdEligible).Distinct();
		foreach (var letter in groupLetters)
		{
			var direct = directByGroup.TryGetValue(letter, out var ds) ? ds : [];
			var elig = thirdEligible.Contains(letter);
			bool? advanced = null;
			if (elig && allGroupGamesPlayed)
			{
				var thirdPlaceTeamId = c.Standings(letter).ElementAtOrDefault(2)?.TeamId;
				if (thirdPlaceTeamId is not null)
				{
					advanced = c.Games.OfType<FlatKoGame>().Any(ko =>
						(ko.HomeTeamId == thirdPlaceTeamId && FlatQualifierParser.TryParse(ko.HomeQual, out var qh) && qh is FlatThirdPlacePool) ||
						(ko.AwayTeamId == thirdPlaceTeamId && FlatQualifierParser.TryParse(ko.AwayQual, out var qa) && qa is FlatThirdPlacePool));
				}
			}
			map[letter] = new GroupQualSlots(direct, elig, advanced);
		}
		return map;
	}

	static void AddQualifier(string dsl, Dictionary<string, HashSet<int>> directByGroup, HashSet<string> thirdEligible)
	{
		if (!FlatQualifierParser.TryParse(dsl, out var q) || q is null) { return; }
		switch (q)
		{
			case FlatGroupPlacement gp:
				if (!directByGroup.TryGetValue(gp.GroupLetter, out var set)) { directByGroup[gp.GroupLetter] = set = []; }
				set.Add(gp.Place);
				break;
			case FlatThirdPlacePool pool:
				foreach (var letter in pool.EligibleGroups) { thirdEligible.Add(letter); }
				break;
		}
	}
}
