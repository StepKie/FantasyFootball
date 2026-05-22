namespace FantasyFootball.Models;

/// <summary>
/// New-model round metadata. `Flat` prefix is transient (see <see cref="FlatStage"/>).
///
/// Single source of truth for a round's display name, ordering, and stage
/// assignment. Games reference Round by Id; StageId is an FK into the
/// containing Competition's Stages list.
/// </summary>
public sealed record FlatRound(string Id, string Name, string StageId, int Order);
