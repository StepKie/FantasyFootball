namespace FantasyFootball.Models;

[Table(nameof(Round))]
public class Round : NamedUniqueId
{
	[Ignore]
	public List<Game> AllGames => [.. RegularGames, .. KoGames];

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<Game> RegularGames { get; init; } = [];

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<KoGame> KoGames { get; init; } = [];

	[ForeignKey(typeof(Stage))]
	public int StageId { get; set; }

	[ManyToOne]
	public Stage Stage { get; set; }

	[Ignore]
	public bool IsFinished => CurrentGame is null;

	[Ignore]
	public Game? CurrentGame => AllGames.FirstOrDefault(g => !g.IsFinished);

	/// <summary> This should not be asked of a Round </summary>
	[Ignore]
	public Round? NextRoundInStage => Stage.Rounds.FirstOrDefault(r => Stage.Rounds.IndexOf(this) + 1 == Stage.Rounds.IndexOf(r));
}
