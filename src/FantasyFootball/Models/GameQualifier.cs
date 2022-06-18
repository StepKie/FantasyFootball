namespace FantasyFootball.Models;

[Table(nameof(GameQualifier))]
public class GameQualifier : Qualifier
{
	public int QualifierGameId { get; init; }
	public bool LoserQualifies { get; init; }

	public override Team Get()
	{
		// TODO We could also give a GameId within a competition to a game for it to be easier to read in the Factory
		var game = Competition.GamesByDate[QualifierGameId];
		var qualifier = LoserQualifies ? game.Winner : game.Loser;
		return qualifier ?? new Team() { Name = $"{Res.Winner} {game.Name}" };
	}
}
