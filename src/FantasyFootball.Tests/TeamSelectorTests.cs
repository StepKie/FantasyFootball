namespace FantasyFootball.Tests;

public class TeamSelectorTests : BaseTest
{
	public TeamSelectorTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	// TODO Somehow this will not initialize correctly when run in a suite, only succeeds when run standalone
	// Possibly, other test classes modify the underlying :inmemory: db concurrently
	[Fact]
	public void TestDrawTeamsWeightedByElo()
	{
		var toDraw = 10;
		var fromConfederation = Confederation.UEFA;
		TeamSelector ts = new(Repo.GetAll<Team>());
		var drawn = ts.DrawTeamsWeightedByElo(toDraw, fromConfederation);
		Assert.Equal(toDraw, drawn.Count);
		Assert.All(drawn, t => Assert.True(t.Country.Confederation.Equals(fromConfederation)));
	}
}
