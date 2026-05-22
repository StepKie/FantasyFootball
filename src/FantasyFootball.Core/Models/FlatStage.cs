namespace FantasyFootball.Models;

/// <summary>
/// New-model stage metadata. `Flat` prefix is a transient disambiguator while
/// the old graph-model types still live in this namespace; the cleanup PR
/// renames this to <c>Stage</c> after the old <see cref="Stage"/> is deleted.
///
/// Single source of truth for a stage's display name and ordering. No state,
/// no contained games — games are held in <see cref="FlatCompetition.Games"/>
/// with <c>FlatGame.RoundId</c> as the FK (Round.StageId then resolves to this Stage).
/// </summary>
public sealed record FlatStage(string Id, string Name, int Order);
