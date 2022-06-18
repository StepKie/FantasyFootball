namespace FantasyFootball.Models;

[Table(nameof(GameQualifier))]
public class GameQualifier : Qualifier
{
	public int QualifierGameId { get; init; }
	public bool LoserQualifies { get; init; }

	public Game Game => Competition.GamesByDate[QualifierGameId];

	public override Team? Get() => LoserQualifies ? Game.Winner : Game.Loser;

	public override Team GetStandin() => new() { Name = $"{Res.Winner} {Game.Name}", ShortName = "TBD" };
}
