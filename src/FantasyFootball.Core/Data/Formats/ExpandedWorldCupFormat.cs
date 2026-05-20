namespace FantasyFootball.Data.Formats;

/// <summary>
/// 48-team FIFA World Cup format introduced in 2026.
/// 12 groups of 4 → top 2 of each group + 8 best third-place finishers advance to a 32-team R32.
/// Uses backtracking over the 8 R32 third-place slot constraints to find any feasible assignment.
/// TODO Replace with FIFA's official 495-scenario lookup table for exact bracket fidelity.
/// </summary>
public sealed class ExpandedWorldCupFormat : ITournamentFormat
{
	public static readonly ExpandedWorldCupFormat Instance = new();
	ExpandedWorldCupFormat() { }

	public int GroupCount => 12;
	public int GroupSize => 4;
	public int AdvancingThirdPlaceCount => 8;

	public IReadOnlyList<string> ThirdPlaceSlotConstraints => _slots;
	static readonly string[] _slots =
	[
		"A/B/C/D/F", // R32 match 74
		"C/D/F/G/H", // R32 match 77
		"C/E/F/H/I", // R32 match 79
		"E/H/I/J/K", // R32 match 80
		"B/E/F/I/J", // R32 match 81
		"A/E/H/I/J", // R32 match 82
		"E/F/G/I/J", // R32 match 85
		"D/E/I/J/L", // R32 match 87
	];

	public Team ResolveThirdPlaceQualifier(Stage groupStage, string thirdPlaceSlot)
	{
		var groups = groupStage.Groups;
		const string letters = "ABCDEFGHIJKL";

		var topEight = groups
			.Select((g, i) => (Letter: letters[i].ToString(), Record: g.GetStandings()[2]))
			.OrderByDescending(t => t.Record)
			.Take(AdvancingThirdPlaceCount)
			.ToList();

		// Backtracking over the 8×8 slot/team bipartite graph — greedy could lock out feasible assignments. Not the FIFA-canonical 495-scenario map; any feasible draw will do.
		var assignments = new Team?[_slots.Length];
		var usedLetters = new HashSet<string>();
		if (!TryAssign(0))
		{
			throw new InvalidOperationException(
				$"No feasible 3rd-place assignment for top-8: {string.Join(", ", topEight.Select(t => $"{t.Letter}={t.Record.Team.ShortName}"))}.");
		}

		Log.Debug($"WC 2026 third-place assignments: {string.Join(", ", _slots.Zip(assignments).Select(p => $"{p.First}={p.Second?.ShortName}"))}");

		var slotIndex = Array.IndexOf(_slots, thirdPlaceSlot);
		return slotIndex >= 0 && assignments[slotIndex] is { } team
			? team
			: throw new InvalidOperationException($"Unknown 3rd-place slot {thirdPlaceSlot}");

		bool TryAssign(int slotIndex)
		{
			if (slotIndex == _slots.Length) { return true; }
			var allowed = _slots[slotIndex].Split('/');
			foreach (var candidate in topEight)
			{
				if (usedLetters.Contains(candidate.Letter)) { continue; }
				if (!allowed.Contains(candidate.Letter)) { continue; }
				assignments[slotIndex] = candidate.Record.Team;
				usedLetters.Add(candidate.Letter);
				if (TryAssign(slotIndex + 1)) { return true; }
				usedLetters.Remove(candidate.Letter);
				assignments[slotIndex] = null;
			}
			return false;
		}
	}
}
