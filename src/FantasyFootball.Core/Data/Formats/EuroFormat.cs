namespace FantasyFootball.Data.Formats;

/// <summary>
/// 24-team UEFA European Championship format used since 2016.
/// 6 groups of 4 → top 2 of each group + 4 best third-place finishers advance to a 16-team R16.
/// </summary>
public sealed class EuroFormat : ITournamentFormat
{
	public static readonly EuroFormat Instance = new();
	EuroFormat() { }

	public int GroupCount => 6;
	public int GroupSize => 4;
	public int AdvancingThirdPlaceCount => 4;

	public IReadOnlyList<string> ThirdPlaceSlotConstraints => _slotsInOrder;
	static readonly string[] _slotsInOrder = ["A/D/E/F", "D/E/F", "A/B/C", "A/B/C/D"];

	static readonly Dictionary<string, int> _slotToId = new()
	{
		["A/D/E/F"] = 0,
		["D/E/F"] = 1,
		["A/B/C"] = 2,
		["A/B/C/D"] = 3,
	};

	/// <summary>
	/// UEFA's deterministic 4-of-6 third-place lookup table. Keyed on the sorted tuple of
	/// the four group indices (1-based) whose third-place teams advanced.
	/// </summary>
	static readonly Dictionary<(int, int, int, int), int[]> _thirdPlaceCombinations = new()
	{
		[(1, 2, 3, 4)] = [1, 4, 3, 2],
		[(1, 2, 3, 5)] = [1, 5, 3, 2],
		[(1, 2, 3, 6)] = [1, 6, 3, 2],
		[(1, 2, 4, 5)] = [4, 5, 2, 1],
		[(1, 2, 4, 6)] = [4, 6, 2, 1],
		[(1, 2, 5, 6)] = [5, 6, 1, 2],
		[(1, 3, 4, 5)] = [5, 4, 1, 3],
		[(1, 3, 4, 6)] = [6, 4, 1, 3],
		[(1, 3, 5, 6)] = [5, 6, 1, 3],
		[(1, 4, 5, 6)] = [5, 6, 1, 4],
		[(2, 3, 4, 5)] = [5, 4, 3, 2],
		[(2, 3, 4, 6)] = [6, 4, 2, 3],
		[(2, 3, 5, 6)] = [6, 5, 2, 3],
		[(2, 4, 5, 6)] = [6, 5, 2, 4],
		[(3, 4, 5, 6)] = [6, 5, 3, 4],
	};

	public Team ResolveThirdPlaceQualifier(Stage groupStage, string thirdPlaceSlot)
	{
		var groups = groupStage.Groups;

		var bestFour = groups
			.Select(g => g.GetStandings()[2])
			.OrderByDescending(record => record)
			.Take(4)
			.Select(record => record.Team)
			.ToList();

		// Track the index when locating the group rather than calling List.IndexOf afterward —
		// IndexOf relies on default Equals which returns false for unsaved Groups (Id == 0).
		int[] qualifierGroupIndices =
		[
			.. bestFour
				.Select(team => groups
					.Select((g, i) => (Group: g, OneBasedIndex: i + 1))
					.First(x => x.Group.Teams.Contains(team))
					.OneBasedIndex)
				.OrderBy(i => i),
		];

		Log.Debug($"Best 4 3rd-place finishers: {string.Join(",", bestFour)}, group ids: {string.Join(",", qualifierGroupIndices)}");
		var key = (qualifierGroupIndices[0], qualifierGroupIndices[1], qualifierGroupIndices[2], qualifierGroupIndices[3]);
		var realizedCombination = _thirdPlaceCombinations[key];
		var groupIndex = realizedCombination[_slotToId[thirdPlaceSlot]] - 1;
		var team = groups[groupIndex].GetStandings()[2].Team;
		Log.Debug($"Qualifier for {thirdPlaceSlot} is {team}");
		return team;
	}
}
