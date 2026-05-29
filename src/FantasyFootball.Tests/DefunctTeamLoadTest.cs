using FantasyFootball.Models;

namespace FantasyFootball.Tests;

public class DefunctTeamLoadTest(ITestOutputHelper output) : BaseTest(output)
{
	[Theory]
	[InlineData("FRG", "West Germany", 2076)]
	[InlineData("GDR", "East Germany", 1791)]
	[InlineData("URS", "Soviet Union", 1924)]
	[InlineData("YUG", "Yugoslavia", 1935)]
	[InlineData("TCH", "Czechoslovakia", 1846)]
	[InlineData("SCG", "Serbia and Montenegro", 1717)]
	[InlineData("ZAI", "Zaire", 1508)]
	public void DefunctTeam_LoadsWithCorrectCodeAndElo(string code3, string englishName, int elo)
	{
		var team = DataService.AllTeams.FirstOrDefault(t => t.Country.Code3 == code3);

		_ = team ?? throw new Xunit.Sdk.XunitException($"Defunct team {code3} ({englishName}) not loaded from the seed.");
		ActiveEloSet.EloOf(team).Should().Be(elo);
	}
}
