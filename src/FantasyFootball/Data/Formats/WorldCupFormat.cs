namespace FantasyFootball.Data.Formats;

/// <summary>
/// 32-team FIFA World Cup format used 1998–2022.
/// 8 groups of 4 → top 2 of each group advance directly to a 16-team R16. No third-place advancement.
/// </summary>
public sealed class WorldCupFormat : ITournamentFormat
{
	public static readonly WorldCupFormat Instance = new();
	WorldCupFormat() { }

	public int GroupCount => 8;
	public int GroupSize => 4;
	public int AdvancingThirdPlaceCount => 0;
	public IReadOnlyList<string> ThirdPlaceSlotConstraints { get; } = [];

	public Team ResolveThirdPlaceQualifier(Stage groupStage, string thirdPlaceSlot) =>
		throw new NotSupportedException("World Cup pre-2026 format does not advance third-place teams.");
}
