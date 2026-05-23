namespace FantasyFootball.Models;

/// <summary>
/// Stage metadata.
///
/// Single source of truth for a stage's display name and ordering. No state,
/// no contained games — games are held in <see cref="Competition.Games"/>
/// with <c>Game.RoundId</c> as the FK (Round.StageId then resolves to this Stage).
/// </summary>
public sealed record Stage(string Id, string Name, int Order);
