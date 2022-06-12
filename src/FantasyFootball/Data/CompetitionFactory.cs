namespace FantasyFootball.Data;

public abstract class CompetitionFactory
{
	protected readonly IList<Team> _participantPool;
	Competition _competition;

	protected CompetitionType CompetitionType { get; init; }
	protected DateTime StartDate { get; init; }
	public ITeamSelector TeamSelector { get; init; }
	public List<Group> Groups { get; set; }

	public IList<Team> Participants { get; set; }

	public CompetitionFactory(CompetitionType type, DateTime startDate, IRepository repo)
	{
		_participantPool = repo.GetAll<Team>();
		CompetitionType = type;
		StartDate = startDate;
	}

	protected abstract void CreateParticipants();
	protected abstract void CreateGroups();
	protected abstract IList<Stage> CreateStages();

	[Time]
	/// <summary>
	/// Creates an (unsaved) competition
	/// </summary>
	/// <returns></returns>
	public virtual Competition Create()
	{
		CreateGroups();
		_competition = new()
		{
			Name = CompetitionType.Name().Long + " 2020",
			ShortName = CompetitionType.Name().Short + " 2020",
			SimulationStart = DateTime.Now,
			Stages = CreateStages().ToList(),

		};

		return _competition;
	}

	public Team Team(string shortName) => _participantPool.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
}
