namespace FantasyFootball.Tests;

public class GroupFactoryTests : BaseTest
{
	public GroupFactoryTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	// TODO Somehow this will not initialize correctly when run in a suite, only succeeds when run standalone
	// Possibly, other test classes modify the underlying :inmemory: db concurrently
	[Fact]
	public void TestDrawTeamsWeightedByElo()
	{
		var fromConfederation = Confederation.UEFA;
		var groups = GroupFactory.Random(DataService, CompetitionType.EM).CreateGroups();
		var drawn = groups.SelectMany(g => g.Teams).ToList();
		Assert.Equal(24, drawn.Count);
		Assert.All(drawn, t => Assert.True(t.Country.Confederation.Equals(Confederation.UEFA)));
	}
}
