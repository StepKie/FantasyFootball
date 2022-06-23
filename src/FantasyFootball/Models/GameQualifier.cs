namespace FantasyFootball.Models;

[Table(nameof(GameQualifier))]
public class GameQualifier : Qualifier
{
	public int GameNoInCompetition { get; init; }
	public bool LoserQualifies { get; init; }

	public Game? QualifierGame => Competition?.GamesByDate[GameNoInCompetition - 1];

	public override Team? Get() => LoserQualifies ? QualifierGame?.Loser : QualifierGame?.Winner;

	public override Team GetStandin() => new() { Name = $"{Res.Winner} {QualifierGame?.Name ?? GameNoInCompetition.ToString()}", ShortName = "TBD" };
}
