namespace FantasyFootball.Tests;

public class MatchProbabilityTests
{
	[Fact]
	public void EqualElos_HomeAndAwayMirror_DrawNonTrivial()
	{
		var p = MatchProbability.WinDrawLoss(1500, 1500);

		p.Home.Should().BeApproximately(p.Away, 1e-9);
		p.Draw.Should().BeGreaterThan(0.15);
		(p.Home + p.Draw + p.Away).Should().BeApproximately(1.0, 1e-9);
	}

	[Fact]
	public void StrongHomeFavorite_HomeProbDominates()
	{
		var p = MatchProbability.WinDrawLoss(1900, 1300);

		p.Home.Should().BeGreaterThan(0.75);
		p.Away.Should().BeLessThan(0.10);
	}

	[Fact]
	public void StrongAwayFavorite_AwayProbDominates()
	{
		var p = MatchProbability.WinDrawLoss(1300, 1900);

		p.Away.Should().BeGreaterThan(0.75);
		p.Home.Should().BeLessThan(0.10);
	}
}
