namespace FantasyFootball.Models;

/// <summary>
/// Games which may not end in a draw, and usually have a Qualifier from a previous round in a tournament structure
/// </summary>
[Table(nameof(KoGame))]
public class KoGame : Game
{
	Team? _homeTeam;
	Team? _awayTeam;

	readonly Qualifier _homeQualifier;
	readonly Qualifier _awayQualifier;

	public KoGame()
	{
		// TODO Control access?
	}

	public override Team HomeTeam
	{
		// When accessing this property, try to fetch the value from the Qualifier and assign to backing field
		// If it is still null, return the standin
		// This is so convoluted because this is kind of a lazy initialized property, but it might be called several times before the initialization occurs
		get
		{
			var team = _homeTeam ??= _homeQualifier.Get();
			return team ?? _homeQualifier.GetStandin();
		}
		init => _homeTeam = value;
	}

	public override Team AwayTeam
	{
		// When accessing this property, try to fetch the value from the Qualifier and assign to backing field
		// If it is still null, return the standin
		// This is so convoluted because this is kind of a lazy initialized property, but it might be called several times before the initialization occurs
		get
		{
			var team = _awayTeam ??= _awayQualifier.Get();
			return team ?? _awayQualifier.GetStandin();
		}
		init => _awayTeam = value;
	}

	public KoGame(int idInCompetition, Qualifier qualifierHome, Qualifier qualifierAway, DateTime playedOn)
	{
		PlayedOn = playedOn;
		_homeQualifier = qualifierHome;
		_awayQualifier = qualifierAway;

	}

	public override void Simulate()
	{
		base.Simulate();

		//TODO Extra time hack
		if (IsKo && HomeScore == AwayScore)
		{
			State = GameState.IN_PROGRESS;
			Ending = GameEnd.EXTRA_TIME;

			var rd = new Random().NextDouble();

			if (rd < 0.1)
			{
				HomeScore += 2;
			}
			else if (rd < 0.5)
			{
				HomeScore += 1;
			}
			else if (rd < 0.9)
			{
				AwayScore += 1;
			}
			else
			{
				AwayScore += 2;
			}
		}

		State = GameState.FINISHED;
	}
}
