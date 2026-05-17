namespace FantasyFootball.Data.Formats;

/// <summary>
/// 48-team FIFA World Cup format introduced in 2026.
/// 12 groups of 4 → top 2 of each group + 8 best third-place finishers advance to a 32-team R32.
/// Uses most-constrained-first greedy assignment over the 8 R32 third-place slot constraints.
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

		// Track used groups by Letter (string), not by tuple — TeamRecord.Equals returns false
		// for unsaved records (Id == 0), so removing tuples by value would silently fail.
		var used = new HashSet<string>();
		var assignments = new Dictionary<string, Team>();
		var pending = _slots.ToList();

		while (pending.Count > 0)
		{
			// Most-constrained slot first: fewest remaining candidates
			var slot = pending
				.OrderBy(s => topEight.Count(t => s.Split('/').Contains(t.Letter) && !used.Contains(t.Letter)))
				.First();

			var allowed = slot.Split('/');
			var pick = topEight
				.Where(t => allowed.Contains(t.Letter) && !used.Contains(t.Letter))
				.OrderByDescending(t => t.Record)
				.FirstOrDefault();

			if (pick.Record is not null)
			{
				assignments[slot] = pick.Record.Team;
				used.Add(pick.Letter);
			}
			pending.Remove(slot);
		}

		Log.Debug($"WC 2026 third-place assignments: {string.Join(", ", assignments.Select(kv => $"{kv.Key}={kv.Value.ShortName}"))}");

		return assignments.TryGetValue(thirdPlaceSlot, out var assigned)
			? assigned
			: throw new InvalidOperationException($"No 3rd-place team could be assigned to slot {thirdPlaceSlot}");
	}
}
