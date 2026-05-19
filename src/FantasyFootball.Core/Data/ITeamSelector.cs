namespace FantasyFootball.Data;

public interface ITeamSelector
{
	IList<Team> GetTeams(int amount);
}
