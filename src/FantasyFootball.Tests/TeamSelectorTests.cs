namespace FantasyFootball.Tests;

public class GroupFactoryTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{

	// TODO Somehow this will not initialize correctly when run in a suite, only succeeds when run standalone
	// Possibly, other test classes modify the underlying :inmemory: db concurrently
	[Fact]
	public void TestDrawTeamsWeightedByElo()
	{
		var fromConfederation = Confederation.UEFA;
		var groups = GroupFactory.For(DataService, CompetitionType.EM).DrawRandom();
		var drawn = groups.SelectMany(g => g.Teams).ToList();
		drawn.Should().HaveCount(24).And.OnlyContain(t => t.Country.Confederation.Equals(Confederation.UEFA));
	}
}
