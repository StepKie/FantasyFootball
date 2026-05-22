using FantasyFootball.Data;
using FantasyFootball.Data.Formats;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Bridges the old graph-shaped <see cref="Competition"/> model to the
/// new <see cref="FlatCompetition"/>. Transitional code: exists only
/// while the old factories live alongside the new JSON-driven flow;
/// deleted in the cleanup PR.
///
/// Why it exists:
/// <list type="bullet">
///   <item>Lets us serialize existing C#-defined formats (WC2022, WC2026,
///         EM2024) into the new JSON shape without hand-transcribing 64+
///         games per competition.</item>
///   <item>Provides the head-to-head comparison surface: convert old →
///         new and assert the two represent the same schedule.</item>
/// </list>
///
/// Stage / round IDs are derived from positional structure of the old
/// graph (Stage 0 = group, Stage 1 = KO; group rounds numbered 1..N;
/// KO rounds slugged by game count + position). Display names are
/// copied verbatim from the old <c>Round.Name</c> / <c>Stage.Name</c>.
/// </summary>
public static class FlatDefinitionConverter
{
	/// <summary>
	/// Build a <see cref="FlatCompetition"/> from an old graph-shaped
	/// <see cref="Competition"/>. The caller supplies <paramref name="year"/>
	/// explicitly because some old factories (notably EM) hard-code template
	/// dates from a different year than the competition itself (EM2024 uses
	/// 2016 dates) — the games' PlayedOn isn't a reliable year source.
	/// </summary>
	public static FlatCompetition Convert(Competition oldComp, string definitionId, int year)
	{
		var stages = ConvertStages(oldComp.Stages);
		var rounds = ConvertRounds(oldComp.Stages);
		var groupAssignments = ConvertGroups(oldComp.Stages[0].Groups);
		var games = ConvertGames(oldComp, rounds);

		var formatId = DeriveFormatId(oldComp.Type, oldComp.Stages[0].Groups.Count);

		return new FlatCompetition
		{
			Id = 0,
			Title = $"{Pretty(oldComp.Type)} {year}",
			Type = oldComp.Type,
			Year = year,
			DefinitionId = definitionId,
			FormatId = formatId,
			GroupAssignments = groupAssignments,
			Stages = stages,
			Rounds = rounds,
			Games = games,
		};
	}

	static FlatStage[] ConvertStages(IReadOnlyList<Stage> oldStages)
	{
		// Convention: index 0 = group, index 1 = ko. Stays stable across
		// every current format (WC32, WC48, EM24).
		var result = new FlatStage[oldStages.Count];
		for (int i = 0; i < oldStages.Count; i++)
		{
			var slug = i == 0 ? "group" : "ko";
			result[i] = new FlatStage(slug, oldStages[i].Name, i);
		}
		return result;
	}

	static FlatRound[] ConvertRounds(IReadOnlyList<Stage> oldStages)
	{
		var result = new List<FlatRound>();
		int order = 0;

		// Group stage rounds — sequential "group-r1", "group-r2", ...
		var groupStage = oldStages[0];
		for (int i = 0; i < groupStage.Rounds.Count; i++)
		{
			var oldRound = groupStage.Rounds[i];
			result.Add(new FlatRound($"group-r{i + 1}", oldRound.Name, "group", order++));
		}

		// KO stage rounds — slugged by game count + final/third disambiguation
		var koStage = oldStages[1];
		for (int i = 0; i < koStage.Rounds.Count; i++)
		{
			var oldRound = koStage.Rounds[i];
			var slug = DeriveKoRoundSlug(oldRound, i, koStage.Rounds.Count);
			result.Add(new FlatRound(slug, oldRound.Name, "ko", order++));
		}

		return result.ToArray();
	}

	static string DeriveKoRoundSlug(Round oldRound, int positionInKoStage, int koRoundCount)
	{
		var gameCount = oldRound.KoGames.Count;
		var isLast = positionInKoStage == koRoundCount - 1;
		var isSecondToLast = positionInKoStage == koRoundCount - 2;

		// Last single-game round is the final. Second-to-last single-game
		// round (only present in WC formats, not EM) is the third-place game.
		if (gameCount == 1 && isLast) { return "final"; }
		if (gameCount == 1 && isSecondToLast) { return "third"; }

		// Game count → slug for the multi-game rounds.
		return gameCount switch
		{
			32 => "r64",   // hypothetical; no current format
			16 => "r32",
			8 => "r16",
			4 => "qf",
			2 => "sf",
			_ => throw new FormatException($"Cannot derive KO round slug from {gameCount} games in round '{oldRound.Name}'."),
		};
	}

	static string[][] ConvertGroups(IReadOnlyList<Group> oldGroups)
	{
		// Old Group order matches letter order (Group A is index 0, etc.),
		// per the factories' construction order.
		var result = new string[oldGroups.Count][];
		for (int i = 0; i < oldGroups.Count; i++)
		{
			result[i] = oldGroups[i].Teams.Select(t => t.ShortName).ToArray();
		}
		return result;
	}

	static FlatGame[] ConvertGames(Competition oldComp, FlatRound[] newRounds)
	{
		// Iterate by chronological order (GamesByDate). The old GameQualifier
		// references a game by its 1-based index in this same order
		// (GameNoInCompetition), so qualifier strings on the new side keep
		// matching after conversion: W-{n} / L-{n} still mean the n-th game
		// chronologically.
		var oldGames = oldComp.GamesByDate.ToList();
		var newGames = new FlatGame[oldGames.Count];

		// Build a (old Round → new Round.Id) lookup. Names match because
		// we copy Round.Name verbatim during ConvertRounds.
		var roundIdByName = newRounds.ToDictionary(r => r.Name, r => r.Id);

		for (int i = 0; i < oldGames.Count; i++)
		{
			var oldGame = oldGames[i];
			var newId = i + 1;
			var roundId = roundIdByName[oldGame.Round.Name];
			var playedOn = oldGame.PlayedOn;

			if (oldGame is KoGame koGame)
			{
				newGames[i] = new FlatKoGame
				{
					Id = newId,
					PlayedOn = playedOn,
					RoundId = roundId,
					VenueId = null,
					HomeQual = QualifierToDsl(koGame.HomeQualifier),
					AwayQual = QualifierToDsl(koGame.AwayQualifier),
				};
			}
			else
			{
				// Regular group game. Old `Game.HomeTeam` / `AwayTeam` are real
				// Team references — convert to short-name IDs.
				var homeTeam = oldGame.HomeTeam?.ShortName
					?? throw new FormatException($"Group game {newId} ({oldGame.Round.Name}) has no home team.");
				var awayTeam = oldGame.AwayTeam?.ShortName
					?? throw new FormatException($"Group game {newId} ({oldGame.Round.Name}) has no away team.");
				var groupLetter = DeriveGroupLetter(oldComp.Stages[0].Groups, oldGame.HomeTeam!);

				newGames[i] = new FlatGroupGame
				{
					Id = newId,
					PlayedOn = playedOn,
					RoundId = roundId,
					VenueId = null,
					GroupLetter = groupLetter,
					HomeTeamId = homeTeam,
					AwayTeamId = awayTeam,
				};
			}
		}

		return newGames;
	}

	static string DeriveGroupLetter(IReadOnlyList<Group> groups, Team team)
	{
		for (int i = 0; i < groups.Count; i++)
		{
			if (groups[i].Teams.Any(t => t.ShortName == team.ShortName))
			{
				return ((char)('A' + i)).ToString();
			}
		}
		throw new FormatException($"Team {team.ShortName} not found in any group.");
	}

	static string QualifierToDsl(Qualifier q) => q switch
	{
		GroupQualifier gq when !string.IsNullOrEmpty(gq.ThirdPlaceCombination) =>
			$"{gq.ThirdPlaceCombination}{gq.FinalPlacement}",
		GroupQualifier gq =>
			$"{(char)('A' + gq.GroupId)}{gq.FinalPlacement}",
		GameQualifier ggq =>
			ggq.LoserQualifies ? $"L-{ggq.GameNoInCompetition}" : $"W-{ggq.GameNoInCompetition}",
		_ => throw new FormatException($"Unknown qualifier type {q.GetType().Name}."),
	};

	static string DeriveFormatId(CompetitionType type, int groupCount) => (type, groupCount) switch
	{
		(CompetitionType.WM, 8) => "world-cup-32",
		(CompetitionType.WM, 12) => "world-cup-48",
		(CompetitionType.EM, 6) => "european-championship-24",
		_ => throw new FormatException($"No known format ID for {type} with {groupCount} groups."),
	};

	static string Pretty(CompetitionType t) => t switch
	{
		CompetitionType.WM => "World Cup",
		CompetitionType.EM => "European Championship",
		_ => t.ToString(),
	};
}
