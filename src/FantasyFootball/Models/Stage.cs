namespace FantasyFootball.Models;


[Table(nameof(Stage))]
public class Stage : NamedUniqueId
{
	/// <summary>
	/// Holds the participants. May only be a single Group (in case of a National League for example ...)
	/// TODO Check if this makes sense, or if this belongs to Competition instead?
	/// For example, K.O. Stage in a World Cup has no Groups anymore ...
	/// </summary>
	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<Group> Groups { get; set; } = new();

	/// <summary>
	/// Example World Cup:
	/// Group stage are Round 1, Round 2, Round 3
	/// K.O. stage, Rounds are Round of 16, Quarterfinal, Semifinal, Final
	/// </summary>
	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<Round> Rounds { get; set; } = new();

	[ForeignKey(typeof(Competition))]
	public int CompetitionId { get; set; }

	[ManyToOne]
	public Competition Competition { get; }

	[Ignore] public Round? CurrentRound => Rounds.FirstOrDefault(r => !r.IsFinished);
	[Ignore] public IList<Game> Games => Rounds.SelectMany(round => round.Games).OrderBy(g => g.PlayedOn).ToList();
	[Ignore] public bool IsFinished => CurrentRound is null;
}
