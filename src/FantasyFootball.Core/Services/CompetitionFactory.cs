using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Turns a <see cref="CompetitionSpec"/> into a fresh
/// <see cref="Competition"/> ready for simulation. Centralizes the
/// "spec → competition" mapping so the bulk-sim runner is a plain loop
/// and the three modes' branching logic lives in one place.
///
/// Each call yields an independent Competition — no shared mutable
/// state. The factory is safe to reuse across many specs.
///
/// whole new model, leaving this as plain <c>CompetitionFactory</c>.
/// </summary>
public sealed class CompetitionFactory
{
	readonly ICompetitionDefinitionStore _definitions;

	public CompetitionFactory(ICompetitionDefinitionStore definitions)
	{
		_definitions = definitions;
	}

	public Competition Create(CompetitionSpec spec)
	{
		var competition = _definitions.Load(spec.DefinitionId);

		if (competition.GroupAssignments.Length == 0 && spec is not HistoricalSpec)
		{
			throw new ArgumentException(
				$"Competition '{spec.DefinitionId}' is knockout-only (no group stage); lineup-substituting specs are not supported. Use HistoricalSpec.",
				nameof(spec));
		}

		switch (spec)
		{
			case HistoricalSpec:
				// Definition file already carries the historical lineup; nothing to do.
				return competition;

			case CustomLineupSpec custom:
				ValidateLineupShape(custom.Groups, competition);
				ApplyLineup(competition, custom.Groups);
				return competition;

			case RandomLineupSpec random:
				var drawn = random.DrawAlgorithm.Draw(
					competition.GroupAssignments.Length,
					competition.GroupAssignments[0].Length);
				ValidateLineupShape(drawn, competition);
				ApplyLineup(competition, drawn);
				return competition;

			default:
				throw new ArgumentException($"Unknown spec type {spec.GetType().Name}.", nameof(spec));
		}
	}

	static void ValidateLineupShape(string[][] lineup, Competition competition)
	{
		var expectedGroups = competition.GroupAssignments.Length;
		var expectedPerGroup = competition.GroupAssignments[0].Length;
		if (lineup.Length != expectedGroups)
		{
			throw new ArgumentException(
				$"Lineup has {lineup.Length} groups; competition '{competition.DefinitionId}' needs {expectedGroups}.");
		}
		for (int i = 0; i < lineup.Length; i++)
		{
			if (lineup[i].Length != expectedPerGroup)
			{
				throw new ArgumentException(
					$"Group {(char)('A' + i)} has {lineup[i].Length} teams; competition '{competition.DefinitionId}' needs {expectedPerGroup} per group.");
			}
		}
		var allIds = lineup.SelectMany(g => g).ToList();
		if (allIds.Distinct().Count() != allIds.Count)
		{
			throw new ArgumentException(
				$"Lineup for '{competition.DefinitionId}' contains duplicate team IDs.");
		}
	}

	static void ApplyLineup(Competition competition, string[][] newLineup)
	{
		// 1. Build old→new mapping based on positional pairing per group:
		//    new[g][t] replaces old[g][t]. Preserves the within-group
		//    pairing of the definition — a game previously "QAT vs ECU"
		//    (positions 0 and 1 of Group A) stays "position-0 vs position-1"
		//    after substitution, just with the new teams in those slots.
		var oldLineup = competition.GroupAssignments;
		var oldToNew = new Dictionary<string, string>(oldLineup.Sum(g => g.Length));
		for (int g = 0; g < oldLineup.Length; g++)
		{
			for (int t = 0; t < oldLineup[g].Length; t++)
			{
				oldToNew[oldLineup[g][t]] = newLineup[g][t];
			}
		}

		// 2. Overwrite GroupAssignments inner arrays in place. The outer
		//    array reference is init-only on Competition; each inner
		//    string[] is mutable.
		for (int g = 0; g < newLineup.Length; g++)
		{
			Array.Copy(newLineup[g], oldLineup[g], newLineup[g].Length);
		}

		// 3. Rewire each GroupGame to the new teams. Records are
		//    immutable, so we replace the array slot with a fresh
		//    record via `with`. KO games don't need rewiring — their
		//    team IDs are unresolved at materialization time; the
		//    simulator fills them in once the qualifier chain resolves.
		for (int i = 0; i < competition.Games.Length; i++)
		{
			if (competition.Games[i] is GroupGame gg)
			{
				competition.Games[i] = gg with
				{
					HomeTeamId = oldToNew[gg.HomeTeamId],
					AwayTeamId = oldToNew[gg.AwayTeamId],
				};
			}
		}
	}
}
