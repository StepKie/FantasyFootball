using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Per-group qualification status for the standings panel: which group
/// positions advance directly to the next stage (via group-placement
/// qualifiers like <c>A1</c>) and whether the group's 3rd-placer is
/// in any 3rd-place pool — and if so, whether they actually advanced.
///
/// Pure projection over a <see cref="Competition"/>: walks every
/// <see cref="KoGame"/>'s qualifier DSL to discover the topology,
/// then consults the current standings to fill in the "advanced?"
/// answer once the group stage has finished.
/// </summary>
public static class GroupQualification
{
	/// <summary>
	/// Qualification slots for a single group.
	/// </summary>
	/// <param name="DirectSlots">
	/// Positions referenced by direct group-placement qualifiers (e.g. position 1 for an <c>A1</c> ref).
	/// </param>
	/// <param name="ThirdPlaceEligible">
	/// True if this group's letter appears in any <see cref="ThirdPlacePool"/>.
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
	public static Dictionary<string, GroupQualSlots> Compute(Competition? c)
	{
		var map = new Dictionary<string, GroupQualSlots>();
		if (c is null) { return map; }

		var directByGroup = new Dictionary<string, HashSet<int>>();
		var thirdEligible = new HashSet<string>();

		foreach (var ko in c.Games.OfType<KoGame>())
		{
			AddQualifier(ko.HomeQual, directByGroup, thirdEligible);
			AddQualifier(ko.AwayQual, directByGroup, thirdEligible);
		}

		// Pool can't resolve until all group games finish; once resolved, the KO slot's team id tells us if this group's 3rd-placer advanced.
		var allGroupGamesPlayed = c.Games.OfType<GroupGame>().All(g => g.Result is not null);
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
					advanced = c.Games.OfType<KoGame>().Any(ko =>
						(ko.HomeTeamId == thirdPlaceTeamId && QualifierParser.TryParse(ko.HomeQual, out var qh) && qh is ThirdPlacePool) ||
						(ko.AwayTeamId == thirdPlaceTeamId && QualifierParser.TryParse(ko.AwayQual, out var qa) && qa is ThirdPlacePool));
				}
			}
			map[letter] = new GroupQualSlots(direct, elig, advanced);
		}
		return map;
	}

	static void AddQualifier(string dsl, Dictionary<string, HashSet<int>> directByGroup, HashSet<string> thirdEligible)
	{
		if (!QualifierParser.TryParse(dsl, out var q) || q is null) { return; }
		switch (q)
		{
			case GroupPlacement gp:
				if (!directByGroup.TryGetValue(gp.GroupLetter, out var set)) { directByGroup[gp.GroupLetter] = set = []; }
				set.Add(gp.Place);
				break;
			case ThirdPlacePool pool:
				foreach (var letter in pool.EligibleGroups) { thirdEligible.Add(letter); }
				break;
		}
	}
}
