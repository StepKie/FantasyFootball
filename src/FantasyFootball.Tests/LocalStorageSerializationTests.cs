using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SQLite;

namespace FantasyFootball.Tests;

/// <summary>
/// Reproduces and pins the LocalStorage round-trip the Web host depends on.
/// Live bug: after creating a competition and simulating a few games, the
/// Web host serializes a JSON blob that fails to deserialize with
/// `MetadataReferenceNotFound` somewhere in `Stages[0].Groups[1]`.
/// </summary>
public class LocalStorageSerializationTests(ITestOutputHelper output) : BaseTest(output)
{
	static JsonSerializerOptions BuildOptions() => new()
	{
		// Same resolver the Web host uses.
		TypeInfoResolver = new TestIgnoreResolver(),
		ReferenceHandler = ReferenceHandler.Preserve,
	};

	[Fact]
	public void CreatedCompetition_FromFactory_RoundTrips()
	{
		// Use the factory output directly, NOT InitCompetition (which goes through
		// SQLite and re-wires the graph via FK joins, hiding the Web bug).
		var competition = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
		var options = BuildOptions();

		var json = JsonSerializer.Serialize(new List<Competition> { competition }, options);
		Output.WriteLine($"Length={json.Length}");

		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
		act.Should().NotThrow();
	}

	[Fact]
	public async Task PartiallySimulated_FromFactory_RoundTrips()
	{
		var competition = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
		var simulator = new CompetitionSimulator(competition, Repo) { Quiet = true };
		for (var i = 0; i < 3; i++) { await simulator.SimulateGame(competition.CurrentGame!); }
		var options = BuildOptions();

		var json = JsonSerializer.Serialize(new List<Competition> { competition }, options);
		Output.WriteLine($"Length={json.Length}");

		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
		act.Should().NotThrow();
	}

	[Fact]
	public void Factory_GroupStage_Is_Same_Instance_As_Group_BackReference()
	{
		// Sanity: WireBackReferences should make Group.Stage refer to the SAME object
		// as competition.Stages[0]. If not, the JSON serializer writes both Stage
		// instances and reference-handler IDs go off-rails.
		var competition = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
		var groupStage = competition.Stages[0];
		foreach (var group in groupStage.Groups)
		{
			object.ReferenceEquals(group.Stage, groupStage)
				.Should().BeTrue($"Group {group.Name}.Stage must be the same instance as competition.Stages[0]");
		}
	}

	[Fact]
	public void WebFlow_GroupsFromHistoricalData_PassedToFactory_RoundTrips()
	{
		// Mirrors the actual web flow: groups come from GroupFactory.CreateFromHistoricalData
		// FIRST, then are passed into CompetitionFactory.For. This is what
		// CompetitionSetupViewModel does (ResetToHistoricTeams → Create), and it
		// differs from CompetitionFactory.Default which calls CreateFromHistoricalData
		// internally — same calls but separated.
		var groups = GroupFactory.For(DataService, CompetitionType.WM, 2026).CreateFromHistoricalData(2026);
		var competition = CompetitionFactory.For(CompetitionType.WM, 2026, groups).Create();

		var groupStage = competition.Stages[0];
		foreach (var group in groupStage.Groups)
		{
			object.ReferenceEquals(group.Stage, groupStage)
				.Should().BeTrue($"Group {group.Name}.Stage must be the same instance as competition.Stages[0]");
		}

		var options = BuildOptions();
		var json = JsonSerializer.Serialize(new List<Competition> { competition }, options);
		Output.WriteLine($"Length={json.Length}");
		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
		act.Should().NotThrow();
	}

	[Fact]
	public void TwoCompetitions_SharingFactoryGroups_RoundTrips()
	{
		// Repro of the live bug: user picks EM 2016, creates Comp A, then clicks
		// "Start new" again without changing year. The Setup VM keeps its Groups
		// field across Create() calls (only ResetToHistoricTeams refreshes it on
		// year change), so Comp B is built from the SAME List<Group> as Comp A.
		// WireBackReferences for Comp B overwrites group.Stage to Comp B's Stage,
		// breaking Comp A's graph. Bucket serialize then writes invalid Preserve
		// JSON with forward $refs → MetadataReferenceNotFound on next load.
		var sharedGroups = GroupFactory.For(DataService, CompetitionType.EM, 2016).CreateFromHistoricalData(2016);
		var compA = CompetitionFactory.For(CompetitionType.EM, 2016, sharedGroups).Create();
		var compB = CompetitionFactory.For(CompetitionType.EM, 2016, sharedGroups).Create();

		var options = BuildOptions();
		var json = JsonSerializer.Serialize(new List<Competition> { compA, compB }, options);
		Output.WriteLine($"Length={json.Length}");

		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
		act.Should().NotThrow();
	}

	[Fact]
	public void AfterDeserialize_GroupStage_Is_Still_Same_Instance_As_Group_BackReference()
	{
		// THE actual web bug: Setup saves a fresh competition (good JSON). Detail
		// LOADS it back from LocalStorage — that's a Deserialize. After deserialize,
		// Group.Stage must STILL be the same instance as competition.Stages[0].
		// If not, the next Save serializes a graph with duplicated Stage instances
		// and produces invalid Preserve JSON with forward $refs.
		var competition = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
		var options = BuildOptions();

		var json1 = JsonSerializer.Serialize(new List<Competition> { competition }, options);
		var roundTripped = JsonSerializer.Deserialize<List<Competition>>(json1, options)!.Single();

		var groupStage = roundTripped.Stages[0];
		foreach (var group in groupStage.Groups)
		{
			object.ReferenceEquals(group.Stage, groupStage)
				.Should().BeTrue($"After deserialize, Group {group.Name}.Stage must STILL be the same instance as competition.Stages[0]");
		}

		// And re-serializing must produce JSON that deserializes again without error.
		var json2 = JsonSerializer.Serialize(new List<Competition> { roundTripped }, options);
		var act = () => JsonSerializer.Deserialize<List<Competition>>(json2, options);
		act.Should().NotThrow();
	}

	/// <summary>
	/// Local copy of the Web host's IgnoreAttributeTypeInfoResolver so this test
	/// project doesn't need to reference Web. Kept in sync by hand — if the
	/// production resolver changes meaningfully, update this too.
	/// </summary>
	sealed class TestIgnoreResolver : DefaultJsonTypeInfoResolver
	{
		static readonly HashSet<(Type DeclaringType, string PropertyName)> BackPointerCollections =
		[
			(typeof(Confederation), nameof(Confederation.Countries)),
			(typeof(Country), nameof(Country.Clubs)),
		];

		public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			var info = base.GetTypeInfo(type, options);
			var toRemove = new List<JsonPropertyInfo>();
			foreach (var property in info.Properties)
			{
				if (property.AttributeProvider is not PropertyInfo propInfo) { continue; }
				var hasExactIgnore = propInfo
					.GetCustomAttributes(typeof(IgnoreAttribute), inherit: false)
					.Any(a => a.GetType() == typeof(IgnoreAttribute));
				var isBackPointer = BackPointerCollections.Contains((type, propInfo.Name));
				if (hasExactIgnore || isBackPointer) { toRemove.Add(property); }
			}
			foreach (var p in toRemove) { info.Properties.Remove(p); }
			return info;
		}
	}
}
