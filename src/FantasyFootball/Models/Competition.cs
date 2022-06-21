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

	/// <summary> TODO Figure out the Cascade behavior with the inverse relationships (ManyToOne). For example, currently Round->Stage is resolved, but Stage->Competition is not?! </summary>
	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<Stage> Stages { get; set; } = new();
	 
	[Ignore] public IList<Group> Groups => Stages.SelectMany(stage => stage.Groups).ToList();
	[Ignore] public List<Team> Participants => Groups.SelectMany(group => group.Teams).ToList();
	[Ignore] public List<Round> Rounds => Stages.SelectMany(stage => stage.Rounds).ToList();
	[Ignore] public IList<Game> GamesByDate => Rounds.SelectMany(round => round.AllGames).OrderBy(g => g.PlayedOn).ToList();
	[Ignore] public Game? LastGame => GamesByDate.LastOrDefault(g => g.IsFinished);

	[Ignore] public Stage? CurrentStage => Stages.FirstOrDefault(s => !s.IsFinished);
	[Ignore] public Game? CurrentGame => GamesByDate.FirstOrDefault(g => !g.IsFinished);

	[Ignore] public bool IsFinished => CurrentStage is null;
	[Ignore] public Team? Winner => IsFinished ? LastGame?.Winner : null;
	[Ignore] public string CurrentStatus => IsFinished ? Winner!.Name : $"{CurrentGame!.Round.Stage.Name}, {CurrentGame!.Round.Name}";
}
