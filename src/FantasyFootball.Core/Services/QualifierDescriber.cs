using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Renders a qualifier DSL token as a human-readable slot label. Used by
/// the detail page to show "Winner of Round of 16 game 1" instead of the
/// raw <c>W-49</c> when a KO game's teams haven't resolved yet.
/// </summary>
public static class QualifierDescriber
{
	public static string Describe(Competition competition, string qualifierDsl) =>
		QualifierParser.TryParse(qualifierDsl, out var q) && q is not null
			? Describe(competition, q)
			: qualifierDsl;

	static string Describe(Competition competition, Qualifier q) => q switch
	{
		GroupPlacement gp => DescribeGroupPlacement(gp),
		GameWinner gw => DescribeGameOutcome(competition, gw.GameId, winner: true),
		GameLoser gl => DescribeGameOutcome(competition, gl.GameId, winner: false),
		ThirdPlacePool pool => $"Best 3rd of {string.Join("/", pool.EligibleGroups)}",
		_ => q.ToString() ?? "",
	};

	static string DescribeGroupPlacement(GroupPlacement gp) => gp.Place switch
	{
		1 => $"Group {gp.GroupLetter} winner",
		2 => $"Group {gp.GroupLetter} runner-up",
		3 => $"Group {gp.GroupLetter} 3rd",
		4 => $"Group {gp.GroupLetter} 4th",
		_ => $"Group {gp.GroupLetter} {gp.Place}",
	};

	static string DescribeGameOutcome(Competition competition, int gameId, bool winner)
	{
		var game = competition.Games.FirstOrDefault(g => g.Id == gameId);
		if (game is null) { return winner ? $"Winner of game {gameId}" : $"Loser of game {gameId}"; }

		var roundShort = RoundShortNames.Of(game.RoundId);
		var position = competition.PositionInRound(game);

		// Final / Third-place are single-game rounds — drop the redundant "#1".
		var sameRoundCount = competition.Games.Count(g => g.RoundId == game.RoundId);
		var verb = winner ? "Winner" : "Loser";
		return sameRoundCount == 1
			? $"{verb} of {roundShort}"
			: $"{verb} of {roundShort} #{position}";
	}
}
