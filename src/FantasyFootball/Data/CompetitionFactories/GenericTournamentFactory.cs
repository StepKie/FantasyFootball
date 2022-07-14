﻿namespace FantasyFootball.Data.CompetitionFactories;

public abstract class GenericTournamentFactory : CompetitionFactory
{
	public GenericTournamentFactory(CompetitionType type, DateTime startDate, IDataService dataService, List<Group> groups) : base(type, startDate, groups)
	{
		Groups = GroupFactory.For(dataService, type).CreateFromHistoricalData(startDate.Year);
	}
}
