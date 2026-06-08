namespace FantasyFootball.Models;

/// <summary>
/// Lightweight projection of a <see cref="Competition"/> for the competitions
/// list — every field the list renders (icon, title, date, winner, state)
/// without the games. Persisted alongside the full competition so the list
/// never has to deserialize thousands of full seasons; the full competition is
/// loaded only when one is opened or for the overall-standings aggregate.
/// </summary>
public sealed record CompetitionSummary(
	int Id,
	CompetitionType Type,
	string Title,
	bool IsNationalTeam,
	DateTime? SimulationStart,
	bool IsFinished,
	string? WinnerId,
	int PlayedGames,
	int TotalGames)
{
	/// <summary>Projects the list-relevant fields off a fully-materialized competition.</summary>
	public static CompetitionSummary Of(Competition c) => new(
		c.Id,
		c.Type,
		c.Title,
		c.IsNationalTeamCompetition(),
		c.SimulationStart,
		c.IsFinished(),
		c.WinnerTeamId(),
		c.Games.Count(g => g.Result is not null),
		c.Games.Length);
}
