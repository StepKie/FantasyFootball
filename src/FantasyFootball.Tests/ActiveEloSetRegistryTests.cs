namespace FantasyFootball.Tests;

public class ActiveEloSetRegistryTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void JsonDataService_Init_SeedsCurrentEloSetAndActivatesIt()
	{
		ActiveEloSet.Current.Should().NotBeNull();
		ActiveEloSet.Current!.Name.Should().Be("Current");
		ActiveEloSet.Current.Snapshot.Should().NotBeEmpty();
		Repo.GetAll<EloSet>().Should().Contain(s => s.Name == "Current");
	}

	[Fact]
	public void JsonDataService_Init_SeedsHistoricalEloSets()
	{
		var sets = Repo.GetAll<EloSet>();

		// 25 historical (WC 1962-2022, EC 1980-2024) + 1 Current.
		sets.Should().HaveCount(26);
		sets.Select(s => s.Name).Should().Contain(["Current", "1986", "2018", "2024"]);

		var wc2018 = sets.Single(s => s.Name == "2018");
		wc2018.Date.Should().Be(new DateOnly(2018, 6, 14));
		wc2018.Snapshot["GER"].Should().BeGreaterThan(1500, "Germany was a top side heading into WC 2018");
		wc2018.Snapshot["BRA"].Should().BeGreaterThan(1500);
	}

	[Fact]
	public void EloOf_ReadsFromActiveSnapshot()
	{
		var registry = new ActiveEloSetRegistry(ActiveEloSet);

		// Cross-reference the live snapshot rather than hardcoding a value that drifts.
		var argFromSnapshot = ActiveEloSet.Current!.Snapshot["ARG"];

		registry.EloOf("ARG").Should().Be(argFromSnapshot);
	}

	[Fact]
	public void SetCurrent_SwitchesEloSource()
	{
		var registry = new ActiveEloSetRegistry(ActiveEloSet);
		var argBefore = registry.EloOf("ARG");

		ActiveEloSet.SetCurrent(new EloSet
		{
			Name = "Test",
			Date = new DateOnly(2026, 1, 1),
			Snapshot = new Dictionary<string, int> { ["ARG"] = argBefore + 500 },
		});

		registry.EloOf("ARG").Should().Be(argBefore + 500);
	}

	[Fact]
	public void EloOf_MissingTeam_Throws()
	{
		ActiveEloSet.SetCurrent(new EloSet
		{
			Name = "Sparse",
			Date = new DateOnly(2026, 1, 1),
			Snapshot = new Dictionary<string, int> { ["BRA"] = 2000 },
		});
		var registry = new ActiveEloSetRegistry(ActiveEloSet);

		Action act = () => registry.EloOf("ARG");

		act.Should().Throw<KeyNotFoundException>().WithMessage("*ARG*Sparse*");
	}
}
