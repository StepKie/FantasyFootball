namespace FantasyFootball.Models;

[Table(nameof(GameQualifier))]
public class GameQualifier : Qualifier
{
	public int QualifierGameId { get; init; }
	public bool LoserQualifies { get; init; }

	public Game? QualifierGame => Competition?.GamesByDate[QualifierGameId];

	public override Team? Get() => LoserQualifies ? QualifierGame?.Winner : QualifierGame?.Loser;

	public override Team GetStandin() => new() { Name = $"{Res.Winner} {QualifierGame.Name}", ShortName = "TBD" };
}
