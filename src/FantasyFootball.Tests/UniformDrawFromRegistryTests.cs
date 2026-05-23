namespace FantasyFootball.Tests;

/// <summary>
/// Confirms the uniform draw produces correctly-shaped lineups, doesn't
/// duplicate teams across groups, and fails loudly when the pool is too
/// small for the requested size.
/// </summary>
public class UniformDrawFromRegistryTests
{
	sealed class StubRegistry : ITeamRegistry
	{
		public required IReadOnlyList<string> AllTeamIds { get; init; }
		public int EloOf(string teamId) => 1500;     // draw algorithm doesn't read ELO
	}

	[Fact]
	public void Draw_ProducesRequestedShape()
	{
		var teams = Enumerable.Range(1, 50).Select(i => $"T{i}").ToArray();
		var draw = new UniformDrawFromRegistry(new StubRegistry { AllTeamIds = teams }, new Random(42));

		var result = draw.Draw(8, 4);

		result.Should().HaveCount(8);
		result.Should().OnlyContain(g => g.Length == 4);
	}

	[Fact]
	public void Draw_NoTeamAppearsTwice()
	{
		var teams = Enumerable.Range(1, 50).Select(i => $"T{i}").ToArray();
		var draw = new UniformDrawFromRegistry(new StubRegistry { AllTeamIds = teams }, new Random(42));

		var picked = draw.Draw(8, 4).SelectMany(g => g).ToArray();

		picked.Should().HaveCount(32);
		picked.Distinct().Should().HaveCount(32, "draw must be without replacement");
	}

	[Fact]
	public void Draw_TooSmallPool_ThrowsWithBothCounts()
	{
		var teams = new[] { "T1", "T2", "T3" };
		var draw = new UniformDrawFromRegistry(new StubRegistry { AllTeamIds = teams });

		Action act = () => draw.Draw(8, 4);
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*3 teams*32*");
	}

	[Fact]
	public void Draw_SameSeedTwice_ProducesSameLineup()
	{
		// Determinism check: useful for reproducible tests and for the
		// future "lock in this draw, re-run with different per-game RNG" flow.
		var teams = Enumerable.Range(1, 50).Select(i => $"T{i}").ToArray();
		var registry = new StubRegistry { AllTeamIds = teams };

		var a = new UniformDrawFromRegistry(registry, new Random(123)).Draw(8, 4);
		var b = new UniformDrawFromRegistry(registry, new Random(123)).Draw(8, 4);

		a.Should().BeEquivalentTo(b);
	}
}
