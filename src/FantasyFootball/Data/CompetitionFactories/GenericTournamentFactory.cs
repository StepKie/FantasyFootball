namespace FantasyFootball.Data.CompetitionFactories;

public abstract class GenericTournamentFactory : CompetitionFactory
{
	public GenericTournamentFactory(CompetitionType type, DateTime startDate, IDataService dataService) : base(type, startDate, null)
	{
		Groups = GroupFactory.Random(dataService, type).CreateGroups();
	}
}
