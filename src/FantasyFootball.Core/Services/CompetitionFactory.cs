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
		// Spec wins when present; otherwise keep the definition's own declared EloSet (e.g. "Bundesliga 2025-2026" baked into the JSON).
		if (spec.EloSetName is not null) { competition.EloSetName = spec.EloSetName; }

		if (competition.GroupAssignments.Length == 0 && spec is not HistoricalSpec)
		{
			throw new ArgumentException(
				$"Competition '{spec.DefinitionId}' is knockout-only (no group stage); lineup-substituting specs are not supported. Use HistoricalSpec.",
				nameof(spec));
		}

		switch (spec)
		{
			case HistoricalSpec { Played: true }:
				// Definition's own lineup, keep the baked results — browse the real outcome.
				break;

			case HistoricalSpec:
				// Definition's own lineup, but re-simulate from scratch.
				ResetPlayState(competition);
				break;

			case CustomLineupSpec custom:
				ValidateLineupShape(custom.Groups, competition);
				ApplyLineup(competition, custom.Groups);
				ResetPlayState(competition);
				break;

			case RandomLineupSpec random:
				var drawn = random.DrawAlgorithm.Draw(
					competition.GroupAssignments.Length,
					competition.GroupAssignments[0].Length);
				ValidateLineupShape(drawn, competition);
				ApplyLineup(competition, drawn);
				ResetPlayState(competition);
				break;

			default:
				throw new ArgumentException($"Unknown spec type {spec.GetType().Name}.", nameof(spec));
		}

		return competition;
	}

	// Clears the definition's baked results: every game's Result, the KO slots (so they re-resolve from qualifiers and lose the historic Attendance), and the sim stamps. Yields a scheduled competition ready to simulate from scratch.
	static void ResetPlayState(Competition competition)
	{
		// Attendance is init-only on Game; replace each array slot via `with` to clear it. Otherwise a re-drawn lineup would carry the historic crowd figure forward (Group games as well as KO).
		for (int i = 0; i < competition.Games.Length; i++)
		{
			var game = competition.Games[i];
			game.Result = null;
			if (game is KoGame ko)
			{
				competition.Games[i] = ko with { HomeTeamId = null, AwayTeamId = null, Attendance = null };
			}
			else if (game.Attendance.HasValue)
			{
				competition.Games[i] = game with { Attendance = null };
			}
		}

		competition.SimulationStart = null;
		competition.SimulationFinished = null;
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
