using MathNet.Numerics.Distributions;

namespace FantasyFootball.Services;

/// <summary>
/// Pre-match outcome probabilities matching <see cref="Game.Simulate"/>'s generative model:
/// total goals ~ Poisson(2.5 + 0.001 * (Elo_home - Elo_away)); each goal independently goes
/// home with probability 10^(diff/400) / (1 + 10^(diff/400)). By Poisson thinning the
/// marginal home/away scores are independent Poisson with λ split by that probability.
/// </summary>
public static class MatchProbability
{
	public readonly record struct WinDrawLossResult(double Home, double Draw, double Away);

	public static WinDrawLossResult WinDrawLoss(int eloHome, int eloAway)
	{
		var diff = eloHome - eloAway;
		var lambdaTotal = System.Math.Max(0.5, 2.5 + 0.001 * diff);
		var pAwayGoal = 1.0 / (1 + System.Math.Pow(10, diff / 400.0));
		var lambdaHome = (1 - pAwayGoal) * lambdaTotal;
		var lambdaAway = pAwayGoal * lambdaTotal;

		const int MaxGoals = 10;
		double pH = 0, pD = 0, pA = 0;
		for (int i = 0; i <= MaxGoals; i++)
		{
			for (int j = 0; j <= MaxGoals; j++)
			{
				var p = Poisson.PMF(lambdaHome, i) * Poisson.PMF(lambdaAway, j);
				if (i > j) { pH += p; }
				else if (i == j) { pD += p; }
				else { pA += p; }
			}
		}

		// Renormalize: truncating at MaxGoals=10 loses ~1e-9 of mass at the goal counts we care about, but make it exact for callers.
		var sum = pH + pD + pA;

		return new WinDrawLossResult(pH / sum, pD / sum, pA / sum);
	}
}
