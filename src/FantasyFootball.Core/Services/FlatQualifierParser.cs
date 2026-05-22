using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Parses qualifier DSL strings into typed <see cref="FlatQualifier"/>
/// records. Pure function; no I/O.
///
/// Recognized forms (uppercase letters, no whitespace):
/// <list type="bullet">
///   <item><c>A1</c>, <c>A2</c>, <c>L1</c> — group placement (letter + digits, no dash).</item>
///   <item><c>W-49</c> — game winner (<c>W</c>, dash, digits).</item>
///   <item><c>L-61</c> — game loser (<c>L</c>, dash, digits).</item>
///   <item><c>A/B/F3</c> — 3rd-place pool (letters separated by <c>/</c>, ending with <c>3</c>).</item>
/// </list>
///
/// The dash in <c>W-n</c> / <c>L-n</c> disambiguates from group L
/// placements (<c>L1</c> = group L 1st place; <c>L-1</c> = loser of
/// game 1). Parsing is trivially context-free — no precedence rules
/// or numeric thresholds.
/// </summary>
public static class FlatQualifierParser
{
	public static FlatQualifier Parse(string dsl)
	{
		if (TryParse(dsl, out var result))
		{
			return result!;
		}
		throw new FormatException($"Could not parse qualifier expression '{dsl}'. Recognized forms: A1, W-49, L-61, A/B/F3.");
	}

	public static bool TryParse(string dsl, out FlatQualifier? result)
	{
		result = null;
		if (string.IsNullOrEmpty(dsl)) { return false; }

		// Winner / loser: W-{n} or L-{n}. Dash at position 1; rest is digits.
		if (dsl.Length >= 3 && dsl[1] == '-' && (dsl[0] == 'W' || dsl[0] == 'L'))
		{
			if (!int.TryParse(dsl[2..], out var gameId) || gameId < 1) { return false; }
			result = dsl[0] == 'W' ? new FlatGameWinner(gameId) : new FlatGameLoser(gameId);
			return true;
		}

		// Pool: contains '/' — A/B/F3, A/B/C/D/F3.
		if (dsl.Contains('/'))
		{
			return TryParsePool(dsl, out result);
		}

		// Group placement: {Letter}{n} — e.g. A1, B2, L3.
		// No dash, no slash; first char is letter, rest is digits.
		if (dsl.Length >= 2 && char.IsAsciiLetterUpper(dsl[0]) && int.TryParse(dsl[1..], out var place) && place >= 1)
		{
			result = new FlatGroupPlacement(dsl[0].ToString(), place);
			return true;
		}

		return false;
	}

	static bool TryParsePool(string dsl, out FlatQualifier? result)
	{
		result = null;
		var parts = dsl.Split('/');
		if (parts.Length < 2) { return false; }

		var lastPart = parts[^1];
		if (lastPart.Length < 2 || !char.IsDigit(lastPart[^1])) { return false; }

		// Only place 3 is valid for the pool form (best 3rd-place finishers).
		var place = lastPart[^1] - '0';
		if (place != 3) { return false; }

		var lastLetter = lastPart[..^1];
		if (lastLetter.Length != 1 || !char.IsAsciiLetterUpper(lastLetter[0])) { return false; }

		var letters = new List<string>(parts.Length);
		for (int i = 0; i < parts.Length - 1; i++)
		{
			var p = parts[i];
			if (p.Length != 1 || !char.IsAsciiLetterUpper(p[0])) { return false; }
			letters.Add(p);
		}
		letters.Add(lastLetter);

		result = new FlatThirdPlacePool(letters);
		return true;
	}
}
