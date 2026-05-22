namespace FantasyFootball.Tests;

/// <summary>
/// Unit tests for the qualifier DSL parser. Covers every recognized form
/// plus a representative sample of malformed inputs.
/// </summary>
public class FlatQualifierParserTests
{
	[Theory]
	[InlineData("A1", "A", 1)]
	[InlineData("A2", "A", 2)]
	[InlineData("A3", "A", 3)]
	[InlineData("B1", "B", 1)]
	[InlineData("L1", "L", 1)]    // group L 1st place — unambiguous because no dash
	[InlineData("L3", "L", 3)]    // group L 3rd place
	[InlineData("A12", "A", 12)]  // multi-digit place (unusual but allowed)
	public void Parse_GroupPlacement(string dsl, string expectedLetter, int expectedPlace)
	{
		var q = FlatQualifierParser.Parse(dsl);
		q.Should().BeOfType<FlatGroupPlacement>();
		var gp = (FlatGroupPlacement)q;
		gp.GroupLetter.Should().Be(expectedLetter);
		gp.Place.Should().Be(expectedPlace);
		q.Source.Should().Be(dsl);
	}

	[Theory]
	[InlineData("W-1", 1)]
	[InlineData("W-49", 49)]
	[InlineData("W-104", 104)]
	public void Parse_GameWinner(string dsl, int expectedId)
	{
		var q = FlatQualifierParser.Parse(dsl);
		q.Should().BeOfType<FlatGameWinner>();
		((FlatGameWinner)q).GameId.Should().Be(expectedId);
		q.Source.Should().Be(dsl);
	}

	[Theory]
	[InlineData("L-1", 1)]
	[InlineData("L-61", 61)]
	[InlineData("L-99", 99)]
	public void Parse_GameLoser(string dsl, int expectedId)
	{
		var q = FlatQualifierParser.Parse(dsl);
		q.Should().BeOfType<FlatGameLoser>();
		((FlatGameLoser)q).GameId.Should().Be(expectedId);
		q.Source.Should().Be(dsl);
	}

	[Theory]
	[InlineData("A/B/F3", new[] { "A", "B", "F" })]
	[InlineData("A/B/C/D/F3", new[] { "A", "B", "C", "D", "F" })]
	[InlineData("A/B/F/G/I3", new[] { "A", "B", "F", "G", "I" })]
	public void Parse_ThirdPlacePool(string dsl, string[] expectedLetters)
	{
		var q = FlatQualifierParser.Parse(dsl);
		q.Should().BeOfType<FlatThirdPlacePool>();
		((FlatThirdPlacePool)q).EligibleGroups.Should().BeEquivalentTo(expectedLetters);
		q.Source.Should().Be(dsl);
	}

	[Theory]
	[InlineData("")]
	[InlineData("X")]              // letter only, no digits
	[InlineData("1A")]             // digit-first
	[InlineData("A")]              // letter only
	[InlineData("/A3")]            // pool needs at least two segments
	[InlineData("A//F3")]          // empty middle segment
	[InlineData("A/B/F1")]         // pool form only allows place=3
	[InlineData("a1")]             // lowercase letter
	[InlineData("W-")]             // W with dash but no digits
	[InlineData("L-")]             // L with dash but no digits
	[InlineData("X-49")]           // dash form only valid for W and L
	[InlineData("W-0")]            // game IDs must be ≥ 1
	[InlineData("W-abc")]          // non-numeric after dash
	public void Parse_InvalidInputs_Throw(string dsl)
	{
		Action act = () => FlatQualifierParser.Parse(dsl);
		act.Should().Throw<FormatException>().WithMessage($"*'{dsl}'*");
	}

	[Theory]
	[InlineData("A1")]
	[InlineData("L1")]
	[InlineData("W-49")]
	[InlineData("L-61")]
	[InlineData("A/B/F3")]
	public void TryParse_Roundtrip_PreservesSource(string dsl)
	{
		FlatQualifierParser.TryParse(dsl, out var q).Should().BeTrue();
		q!.Source.Should().Be(dsl);
	}

	[Fact]
	public void TryParse_ReturnsFalse_ForBadInput()
	{
		FlatQualifierParser.TryParse("garbage", out var q).Should().BeFalse();
		q.Should().BeNull();
	}

	[Fact]
	public void Records_HaveValueEquality()
	{
		var a = FlatQualifierParser.Parse("A1");
		var b = FlatQualifierParser.Parse("A1");
		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	[Fact]
	public void GroupL_FirstPlace_ParsesAsPlacement_NotLoser()
	{
		// The whole point of the dash convention: `L1` is unambiguously
		// group L 1st place, never "loser of game 1" (that would be `L-1`).
		var q = FlatQualifierParser.Parse("L1");
		q.Should().BeOfType<FlatGroupPlacement>();
		var gp = (FlatGroupPlacement)q;
		gp.GroupLetter.Should().Be("L");
		gp.Place.Should().Be(1);
	}

	[Fact]
	public void Loser_OfGame1_RequiresDashForm()
	{
		var q = FlatQualifierParser.Parse("L-1");
		q.Should().BeOfType<FlatGameLoser>();
		((FlatGameLoser)q).GameId.Should().Be(1);
	}
}
