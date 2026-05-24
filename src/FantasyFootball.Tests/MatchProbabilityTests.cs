namespace FantasyFootball.Tests;

public class MatchProbabilityTests
{
	[Fact]
	public void EqualElos_HomeAndAwayMirror_DrawNonTrivial()
	{
		var p = MatchProbability.Predict(1500, 1500);

		p.Home.Should().BeApproximately(p.Away, 1e-9);
		p.Draw.Should().BeGreaterThan(0.15);
		(p.Home + p.Draw + p.Away).Should().BeApproximately(1.0, 1e-9);
	}

	[Fact]
	public void StrongHomeFavorite_HomeProbDominates()
	{
		var p = MatchProbability.Predict(1900, 1300);

		p.Home.Should().BeGreaterThan(0.75);
		p.Away.Should().BeLessThan(0.10);
	}

	[Fact]
	public void StrongAwayFavorite_AwayProbDominates()
	{
		var p = MatchProbability.Predict(1300, 1900);

		p.Away.Should().BeGreaterThan(0.75);
		p.Home.Should().BeLessThan(0.10);
	}

	[Fact]
	public void Xg_EqualElos_SumsToBaseLambda_AndIsSymmetric()
	{
		var p = MatchProbability.Predict(1700, 1700);

		p.XgHome.Should().BeApproximately(p.XgAway, 1e-9);
		(p.XgHome + p.XgAway).Should().BeApproximately(2.65, 1e-9);
	}

	[Fact]
	public void Xg_StrongHomeFavorite_HomeXgDominates()
	{
		var p = MatchProbability.Predict(2000, 1500);

		p.XgHome.Should().BeGreaterThan(p.XgAway);
		(p.XgHome + p.XgAway).Should().BeApproximately(3.15, 1e-9, "total λ = 2.65 + 0.001 * 500");
	}

	[Fact]
	public void Xg_IsSymmetricUnderHomeAwaySwap()
	{
		// Same matchup, flipped labels: international tournaments have no home advantage, so xG must just swap.
		var fwd = MatchProbability.Predict(2000, 1500);
		var rev = MatchProbability.Predict(1500, 2000);

		fwd.XgHome.Should().BeApproximately(rev.XgAway, 1e-9);
		fwd.XgAway.Should().BeApproximately(rev.XgHome, 1e-9);
		(fwd.XgHome + fwd.XgAway).Should().BeApproximately(rev.XgHome + rev.XgAway, 1e-9);
	}
}
