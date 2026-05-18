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
	public async Task CreatedCompetition_RoundTrips()
	{
		var wm = InitCompetition(CompetitionType.WM, 2026);
		var options = BuildOptions();

		var json = JsonSerializer.Serialize(new List<Competition> { wm }, options);
		Output.WriteLine($"Length={json.Length}");
		File.WriteAllText(Path.Combine(Path.GetTempPath(), "ff-created.json"), json);

		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
		act.Should().NotThrow();
	}

	[Fact]
	public async Task PartiallySimulated_RoundTrips()
	{
		var wm = InitCompetition(CompetitionType.WM, 2026);
		var simulator = new CompetitionSimulator(wm, Repo) { Quiet = true };
		// Simulate exactly 3 games to mirror "create + a couple of games" reproduction.
		for (var i = 0; i < 3; i++) { await simulator.SimulateGame(wm.CurrentGame!); }
		var options = BuildOptions();

		var json = JsonSerializer.Serialize(new List<Competition> { wm }, options);
		Output.WriteLine($"Length={json.Length}");
		File.WriteAllText(Path.Combine(Path.GetTempPath(), "ff-3sims.json"), json);

		var act = () => JsonSerializer.Deserialize<List<Competition>>(json, options);
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
