namespace FantasyFootball.Models;

[Table(nameof(Round))]
public class Round : NamedUniqueId
{
	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<Game> Games { get; init; } = new();

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<KoGame> KoGames { get; init; } = new();

	[ForeignKey(typeof(Stage))]
	public int StageId { get; set; }

	[ManyToOne]
	public Stage Stage { get; set; }

	[Ignore]
	public bool IsFinished => CurrentGame is null;

	[Ignore]
	public Game? CurrentGame => Games.FirstOrDefault(g => !g.IsFinished);

	/// <summary> This should not be asked of a Round </summary>
	[Ignore]
	public Round? NextRoundInStage => Stage.Rounds.FirstOrDefault(r => Stage.Rounds.IndexOf(this) + 1 == Stage.Rounds.IndexOf(r));
}
