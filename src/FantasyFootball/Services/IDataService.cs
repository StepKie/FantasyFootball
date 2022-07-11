namespace FantasyFootball.Services;

/// <summary>
/// Retrieve data from external sources - APIs, files (csv, txt ...)
/// Provide performant/cached access to data like list of all teams that should not repeatedly be loaded from db in data-intensive contexts
/// </summary>
public interface IDataService
{
	/// <summary> Provides access to all available teams. Implementors should ensure that this is updated whenever a Team is changed (Name, Elo, etc.) </summary>
	List<Team> AllTeams { get; }

	CompetitionType SelectedCompetitionType { get; set; }
	int SelectedCompetitionYear { get; set; }

	List<Team> CreateTeams();

	void Initialize();

	void Reset();
}
