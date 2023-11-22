namespace FantasyFootball.Data;

//TODO Refactor for reuse in other tournaments
public static class EuroRoundAdvancer
{
	public static readonly Dictionary<string, int> combinationToId = new()
	{
		["A/D/E/F"] = 0,
		["D/E/F"] = 1,
		["A/B/C"] = 2,
		["A/B/C/D"] = 3,
	};

	/// <summary>
	/// Assignments to the four possible Round of 16 games according to combinationToId
	/// </summary>
	public static readonly Dictionary<(int, int, int, int), int[]> thirdPlaceCombinations = new()
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

	/// <summary> string is expected in the format of group letters delimited by slashes, e.g. "A/D/E/F" </summary>
	public static Team GetThirdPlaceQualifier(Stage groupStage, string thirdPlaceCombination)
	{
		var groups = groupStage.Groups;

		var thirdPlaceFinishers = groups.Select(g => g.GetStandings()[2]).OrderByDescending(thirdPlaceRecord => thirdPlaceRecord);
		var bestFourThirdPlace = thirdPlaceFinishers.Take(4).Select(r => r.Team);

		int[] qualifierGroupIndices =
		[
			.. bestFourThirdPlace
						.Select(team => groups.First(g => g.Teams.Contains(team)))
						.Select(group => groups.IndexOf(group) + 1)
						.OrderBy(noInCompetition => noInCompetition),
		];

		Log.Debug($"Best 4 3rd place finishers: {string.Join(",", bestFourThirdPlace)}, qualifier group ids ordered: {string.Join(",", qualifierGroupIndices)}");
		(int, int, int, int) asTuple = (qualifierGroupIndices[0], qualifierGroupIndices[1], qualifierGroupIndices[2], qualifierGroupIndices[3]);
		var realizedCombination = thirdPlaceCombinations[asTuple];
		int groupIndex = realizedCombination[combinationToId[thirdPlaceCombination]] - 1;
		var team = groups[groupIndex].GetStandings()[2].Team;
		Log.Debug($"Qualifier for {thirdPlaceCombination} is {team}");

		return team;
	}
}
