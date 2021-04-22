namespace FantasyFootball.Services;

/// <summary>
/// Retrieve data from external sources - APIs, files (csv, txt ...)
/// </summary>
public interface IDataService
{
	IList<Team> CreateTeams();

	/// <summary> Important: Only call once in app lifecycle! </summary>
	void Reset();

}
