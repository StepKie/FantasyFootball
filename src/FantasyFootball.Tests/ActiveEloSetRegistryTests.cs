namespace FantasyFootball.Tests;

public class ActiveEloSetRegistryTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void JsonDataService_Init_SeedsCurrentEloSetAndActivatesIt()
	{
		ActiveEloSet.Current.Should().NotBeNull();
		ActiveEloSet.Current!.Name.Should().Be("Current");
		ActiveEloSet.Current.Snapshot.Should().NotBeEmpty();
		Repo.GetAll<EloSet>().Should().ContainSingle(s => s.Name == "Current");
	}

	[Fact]
	public void EloOf_ReadsFromActiveSnapshot()
	{
		var registry = new ActiveEloSetRegistry(ActiveEloSet, DataService);

		// ARG and BRA are always in the current snapshot; cross-reference against
		// the active EloSet directly to avoid hardcoding a value that drifts.
		var argFromSnapshot = ActiveEloSet.Current!.Snapshot["ARG"];

		registry.EloOf("ARG").Should().Be(argFromSnapshot);
	}

	[Fact]
	public void SetCurrent_SwitchesEloSource()
	{
		var registry = new ActiveEloSetRegistry(ActiveEloSet, DataService);
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
		var registry = new ActiveEloSetRegistry(ActiveEloSet, DataService);

		Action act = () => registry.EloOf("ARG");

		act.Should().Throw<KeyNotFoundException>().WithMessage("*ARG*Sparse*");
	}
}
