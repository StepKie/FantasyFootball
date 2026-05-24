using MathNet.Numerics.Distributions;

namespace FantasyFootball.Services;

/// <summary>
/// Pre-match prediction matching <see cref="EloScoreModel"/>'s generative model:
/// total goals ~ Poisson(2.65 + 0.001 * |Elo_home - Elo_away|); each goal independently
/// goes home with probability 10^(diff/400) / (1 + 10^(diff/400)). By Poisson thinning the
/// marginal home/away scores are independent Poisson with λ split by that probability.
/// Returns win/draw/loss probabilities (via score-grid integration) and the underlying
/// Poisson means as expected goals (xG) per side.
/// </summary>
/// <remarks>
/// Assumes a neutral venue: no home advantage is built in. This is correct for the
/// international tournaments currently simulated (WC, EM — neutral grounds). Once
/// domestic leagues or two-leg KO competitions land, a home-advantage parameter
/// (eloratings.net convention is +100 to the home team's Elo) will need to be
/// threaded through per-competition or per-game.
/// </remarks>
public static class MatchProbability
{
	public readonly record struct Prediction(double Home, double Draw, double Away, double XgHome, double XgAway);

	public static Prediction Predict(int eloHome, int eloAway)
	{
		var diff = eloHome - eloAway;
		// Keep the λ formula aligned with EloScoreModel.SamplePoissonScore.
		var lambdaTotal = 2.65 + 0.001 * System.Math.Abs(diff);
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

		// Renormalize: truncating at MaxGoals=10 loses a tiny fraction of probability mass — near zero for equal Elos, up to ~1% at real-world extremes (|diff|≈1700 puts λ ≈4.4); the renorm makes the H/D/A split exact regardless.
		var sum = pH + pD + pA;

		return new Prediction(pH / sum, pD / sum, pA / sum, lambdaHome, lambdaAway);
	}
}
