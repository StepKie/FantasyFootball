using FantasyFootball.Models;
using MathNet.Numerics.Distributions;

namespace FantasyFootball.Services;

/// <summary>
/// Production score model: samples each match from a Poisson on
/// <c>λ = max(0.5, 2.5 + 0.001 · (eloHome - eloAway))</c> goals total,
/// each goal assigned to home with probability
/// <c>1 - 1/(1 + 10^(eloDiff/400))</c>. Ports the math the old per-game
/// simulator uses, with two fixes: a single seeded <see cref="Random"/>
/// instance instead of <c>new Random()</c> per goal, and an actual
/// extra-time / penalties branch for KO ties.
///
/// KO tie handling:
/// <list type="number">
///   <item>30 minutes of extra time at one-third the base lambda
///         (30/90 = ⅓). If a goal lands, that decides.
///         <c>Ending</c> set to <see cref="GameEnd.EXTRA_TIME"/>.</item>
///   <item>Penalty shootout — one extra goal goes to home with the
///         same per-goal probability as regular time.
///         <c>Ending</c> set to <see cref="GameEnd.PENALTIES"/>.</item>
/// </list>
/// </summary>
public sealed class EloScoreModel : IScoreModel
{
	readonly IFlatTeamRegistry _registry;
	readonly Random _rng;

	public EloScoreModel(IFlatTeamRegistry registry, Random? rng = null)
	{
		_registry = registry;
		_rng = rng ?? Random.Shared;
	}

	public FlatResult ScoreGroupGame(string homeTeamId, string awayTeamId)
	{
		var (h, a) = SamplePoissonScore(homeTeamId, awayTeamId, lambdaFactor: 1.0);
		return new FlatResult(h, a, GameEnd.NORMAL);
	}

	public FlatResult ScoreKoGame(string homeTeamId, string awayTeamId)
	{
		var (h, a) = SamplePoissonScore(homeTeamId, awayTeamId, lambdaFactor: 1.0);
		if (h != a) { return new FlatResult(h, a, GameEnd.NORMAL); }

		// Extra time: 30 minutes at 1/3 the base lambda.
		var (eh, ea) = SamplePoissonScore(homeTeamId, awayTeamId, lambdaFactor: 1.0 / 3.0);
		h += eh; a += ea;
		if (h != a) { return new FlatResult(h, a, GameEnd.EXTRA_TIME); }

		// Penalties: one extra goal goes to home with same per-goal probability.
		var pHome = HomeGoalProbability(homeTeamId, awayTeamId);
		if (_rng.NextDouble() < pHome) { h++; } else { a++; }
		return new FlatResult(h, a, GameEnd.PENALTIES);
	}

	(int Home, int Away) SamplePoissonScore(string homeTeamId, string awayTeamId, double lambdaFactor)
	{
		var eloDiff = _registry.EloOf(homeTeamId) - _registry.EloOf(awayTeamId);
		var lambda = Math.Max(0.5, 2.5 + 0.001 * eloDiff) * lambdaFactor;
		var totalGoals = new Poisson(lambda, _rng).Sample();

		var pHome = HomeGoalProbabilityFromDiff(eloDiff);
		int home = 0, away = 0;
		for (int i = 0; i < totalGoals; i++)
		{
			if (_rng.NextDouble() < pHome) { home++; } else { away++; }
		}
		return (home, away);
	}

	double HomeGoalProbability(string homeTeamId, string awayTeamId) =>
		HomeGoalProbabilityFromDiff(_registry.EloOf(homeTeamId) - _registry.EloOf(awayTeamId));

	static double HomeGoalProbabilityFromDiff(int eloDiff) =>
		1.0 - 1.0 / (1 + Math.Pow(10, eloDiff / 400.0));
}
