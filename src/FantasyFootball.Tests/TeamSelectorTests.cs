using FantasyFootball.Data;
using FantasyFootball.Models;

namespace FantasyFootball.Tests;

public class TeamSelectorTests : BaseTest
{
	public TeamSelectorTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

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
