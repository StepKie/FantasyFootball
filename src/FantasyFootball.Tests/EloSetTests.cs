using System.Text.Json;

namespace FantasyFootball.Tests;

/// <summary>
/// Verifies the EloSet entity round-trips through the JSON path
/// LocalStorageRepository uses (System.Text.Json with web defaults).
/// EloSet is JSON-only — no SQLite attributes — so this is the actual
/// persistence shape, not a proxy for it.
/// </summary>
public class EloSetTests
{
	static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

	[Fact]
	public void RoundTrip_PreservesNameDateAndSnapshot()
	{
		var original = new EloSet
		{
			Id = 7,
			Name = "1986",
			Date = new DateOnly(1986, 5, 31),
			Snapshot = new Dictionary<string, int>
			{
				["BRA"] = 2065,
				["ARG"] = 2010,
				["GER"] = 1990,
			},
		};

		var json = JsonSerializer.Serialize(original, WebJson);
		var loaded = JsonSerializer.Deserialize<EloSet>(json, WebJson);

		loaded.Should().NotBeNull();
		loaded!.Id.Should().Be(7);
		loaded.Name.Should().Be("1986");
		loaded.Date.Should().Be(new DateOnly(1986, 5, 31));
		loaded.Snapshot.Should().BeEquivalentTo(original.Snapshot);
	}

	[Fact]
	public void Snapshot_DefaultsToEmpty()
	{
		var fresh = new EloSet { Name = "Empty", Date = new DateOnly(2026, 1, 1) };

		fresh.Snapshot.Should().NotBeNull().And.BeEmpty();
	}
}
