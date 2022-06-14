namespace FantasyFootball.Data;

public abstract class CompetitionFactory
{
	protected IList<Team> _participantPool;
	protected readonly IRepository _repo;
	Competition _competition;

	protected CompetitionType CompetitionType { get; init; }
	protected DateTime StartDate { get; init; }
	public ITeamSelector TeamSelector { get; init; }
	public List<Group> Groups { get; set; }

	public IList<Team> Participants { get; set; }

	public CompetitionFactory(CompetitionType type, DateTime startDate, IRepository repo)
	{
		_repo = repo;
		CompetitionType = type;
		StartDate = startDate;
	}

	protected abstract List<Team> SelectParticipants();
	protected abstract List<Group> CreateGroups();
	protected abstract List<Stage> CreateStages();

	[Time]
	/// <summary>
	/// Creates an (unsaved) competition
	/// </summary>
	/// <returns></returns>
	public virtual async Task<Competition> Create()
	{
		_participantPool = await _repo.GetAllAsync<Team>();
		Participants = SelectParticipants();
		Groups = CreateGroups();
		_competition = new()
		{
			Name = CompetitionType.Name().Long + " 2020",
			ShortName = CompetitionType.Name().Short + " 2020",
			SimulationStart = DateTime.Now,
			Stages = CreateStages(),
		};

		return _competition;
	}

	public Team Team(string shortName) => Participants.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
}
