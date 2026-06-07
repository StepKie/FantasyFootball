using System.Reflection;
using System.Text.Json;
using FantasyFootball.UI.Helpers;

namespace FantasyFootball.Tests;

/// <summary>
/// Soften is a pure string transform: known long words gain exactly one soft
/// hyphen, everything else passes through untouched. The corpus test pins the
/// completeness invariant — every long word in the bundled team-name data has
/// a curated break point, so no name can silently fall back to a bare
/// mid-word cut on narrow screens.
/// </summary>
public class DisplayHyphenationTests
{
	const char Shy = (char)0x00AD;

	[Fact]
	public void Soften_KnownLongWord_InsertsSoftHyphen()
	{
		var softened = DisplayHyphenation.Soften("Netherlands");

		softened.Should().Contain(Shy.ToString());
		softened.Replace(Shy.ToString(), "").Should().Be("Netherlands", "the marker must be the only change");
	}

	[Fact]
	public void Soften_MultiWordName_OnlyMappedWordChanges()
	{
		var softened = DisplayHyphenation.Soften("Borussia Mönchengladbach");

		var words = softened.Split(' ');
		words[0].Should().Be("Borussia");
		words[1].Should().Contain(Shy.ToString());
	}

	[Theory]
	[InlineData("Wales")]
	[InlineData("HSV")]
	[InlineData("?")]
	public void Soften_ShortName_PassesThroughUnchanged(string name)
	{
		DisplayHyphenation.Soften(name).Should().Be(name);
	}

	[Fact]
	public void Soften_UnmappedLongWord_PassesThroughUnchanged()
	{
		DisplayHyphenation.Soften("Qwertzuiopia").Should().Be("Qwertzuiopia");
	}

	[Fact]
	public void Soften_EveryLongWordInBundledNames_HasAHyphenationPoint()
	{
		// Words with a real hyphen ("Guinea-Bissau") already carry a break point and are exempt.
		var longWords = LoadEnglishNames()
			.SelectMany(n => n.Split(' '))
			.Where(w => w.Length >= 9 && !w.Contains('-'))
			.Distinct();

		var uncovered = longWords.Where(w => DisplayHyphenation.Soften(w) == w).ToList();

		uncovered.Should().BeEmpty("every long team-name word needs a curated soft-hyphen point or it bare-breaks on narrow screens");
	}

	static IEnumerable<string> LoadEnglishNames()
	{
		var coreAssembly = typeof(Team).Assembly;
		foreach (var resource in new[] { JsonDataService.CountriesFile, JsonDataService.ClubsFile })
		{
			using var stream = coreAssembly.GetManifestResourceStream(resource)
				?? throw new InvalidOperationException($"{resource} not found in embedded resources.");
			using var doc = JsonDocument.Parse(stream);
			foreach (var entry in doc.RootElement.EnumerateArray())
			{
				yield return entry.GetProperty("name").GetProperty("en").GetString()!;
			}
		}
	}
}
