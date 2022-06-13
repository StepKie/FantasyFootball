namespace FantasyFootball.Models;

[Table(nameof(Competition))]
public class Competition : NamedUniqueId
{
	public CompetitionType Type { get; init; }
	public string ShortName { get; init; }

	[Ignore] public DateTime? Start => GamesByDate.FirstOrDefault()?.PlayedOn;

	public DateTime SimulationStart { get; set; }

	/// <summary> Set this to persist the DateTime the Competition simulation was finished </summary>
	public DateTime SimulationFinished { get; set; }

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<Stage> Stages { get; set; } = new();

	[Ignore] public Stage? CurrentStage => Stages.FirstOrDefault(s => !s.IsFinished);
	[Ignore] public bool IsFinished => CurrentStage == null;
	[Ignore] public IList<Group> Groups => Stages.SelectMany(stage => stage.Groups).ToList();
	[Ignore] public IList<Round> Rounds => Stages.SelectMany(stage => stage.Rounds).ToList();
	[Ignore] public IList<Game> GamesByDate => Rounds.SelectMany(round => round.Games).OrderBy(g => g.PlayedOn).ToList();
	[Ignore] public Game? LastGame => GamesByDate.LastOrDefault(g => g.IsFinished);
	[Ignore] public Game? CurrentGame => GamesByDate.FirstOrDefault(g => !g.IsFinished);
	[Ignore] public List<Team> Participants => Groups.SelectMany(group => group.Teams).ToList();
	[Ignore] public Team? Winner => IsFinished ? LastGame?.Winner : null;
}
