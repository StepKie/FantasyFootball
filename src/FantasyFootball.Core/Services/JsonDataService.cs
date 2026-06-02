using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasyFootball.Services;

public class JsonDataService : IDataService
{
	public const string CountriesFile = "FantasyFootball.Resources.Data.countries.json";
	public const string ClubsFile = "FantasyFootball.Resources.Data.clubs.json";
	public const string EloCurrentFile = "FantasyFootball.Resources.Data.elo-current.json";
	public const string HistoricalEloSetPrefix = "FantasyFootball.Resources.Data.EloSets.";

	public const string CurrentEloSetName = "Current";

	/// <summary>Names of the bundled EloSets shipped with the app (Current + every EloSets/*.json resource). Used by the UI to mark them as undeletable.</summary>
	public static readonly IReadOnlySet<string> BundledEloSetNames = LoadBundledEloSetNames();

	static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	List<Team>? _teamCache;
	readonly IRepository _repo;
	readonly string _languageId;

	public JsonDataService(IRepository repo, CultureInfo? language = null)
	{
		_repo = repo;
		_languageId = language?.TwoLetterISOLanguageName ?? "en";
		Initialize();
	}

	public void Initialize()
	{
		if (AllTeams.Count == 0) { Reset(); return; }

		// Missing Current EloSet ⇒ corruption (first-run, hand-edited localStorage). Full reset.
		var current = _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == CurrentEloSetName);
		if (current is null) { Reset(); return; }
	}

	/// <summary> Global selected competition type to sync across all relevant pages </summary>
	public CompetitionType SelectedCompetitionType { get; set; } = CompetitionType.WM;

	public List<Team> AllTeams => _teamCache ??= ReloadTeams();

	List<Country> CreateCountries()
	{
		var seeds = LoadCountrySeeds();
		var confederations = _repo.GetAll<Confederation>();

		return seeds.Select(s => new Country
		{
			Code2 = s.Code2,
			Code3 = s.Code3,
			Name = s.Name.GetValueOrDefault(_languageId) ?? s.Name.GetValueOrDefault("en") ?? s.Code3,
			Confederation = confederations.FirstOrDefault(c => c.Name == s.Confederation) ?? Confederation.UNKNOWN,
		}).ToList();
	}

	public List<Team> CreateTeams()
	{
		var countries = CreateCountries();
		var countryByCode3 = countries.ToDictionary(c => c.Code3, c => c);
		var nationalTeams = countries.Select(country => country.NationalTeam);
		var clubTeams = LoadClubSeeds().Select(s => new Team
		{
			Type = TeamType.CLUB_MEN,
			ShortName = s.Code,
			Name = s.Name.GetValueOrDefault(_languageId) ?? s.Name.GetValueOrDefault("en") ?? s.Code,
			Country = countryByCode3[s.Country],
		});
		return nationalTeams.Concat(clubTeams).ToList();
	}

	public void Reset()
	{
		_teamCache = null;
		_repo.Reset();
		_repo.SaveAll(Confederation.ALL);

		_repo.Save(LoadCurrentEloSet());
		_repo.SaveAll(LoadHistoricalEloSets());
		_repo.SaveAll(CreateTeams());

		SelectedCompetitionType = CompetitionType.WM;

		// Broadcast last so subscribers see the fully-populated repo, not a half-written intermediate.
		MessageBus.Send(new DataResetMessage());
	}

	List<Team> ReloadTeams()
	{
		var teams = _repo.GetAll<Team>();
		Log.Debug("Reloaded teams, repo now has {Count} teams", teams.Count);
		return teams;
	}

	static List<CountrySeed> LoadCountrySeeds()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CountriesFile)
			?? throw new FileNotFoundException($"{CountriesFile} not found in embedded resources");
		return JsonSerializer.Deserialize<List<CountrySeed>>(stream, JsonOpts)
			?? throw new InvalidOperationException($"{CountriesFile} deserialized to null");
	}

	static List<ClubSeed> LoadClubSeeds()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ClubsFile)
			?? throw new FileNotFoundException($"{ClubsFile} not found in embedded resources");
		return JsonSerializer.Deserialize<List<ClubSeed>>(stream, JsonOpts)
			?? throw new InvalidOperationException($"{ClubsFile} deserialized to null");
	}

	static EloSet LoadCurrentEloSet()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EloCurrentFile)
			?? throw new FileNotFoundException($"{EloCurrentFile} not found in embedded resources");
		var eloSet = JsonSerializer.Deserialize<EloSet>(stream, JsonOpts)
			?? throw new InvalidOperationException($"{EloCurrentFile} deserialized to null");
		// Seed-time entity — let the repo assign an Id on first save.
		eloSet.Id = 0;
		return eloSet;
	}

	static List<EloSet> LoadHistoricalEloSets()
	{
		var asm = Assembly.GetExecutingAssembly();
		return asm.GetManifestResourceNames()
			.Where(n => n.StartsWith(HistoricalEloSetPrefix, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
			.Select(n =>
			{
				using var stream = asm.GetManifestResourceStream(n)!;
				var set = JsonSerializer.Deserialize<EloSet>(stream, JsonOpts)
					?? throw new InvalidOperationException($"{n} deserialized to null");
				set.Id = 0;
				return set;
			})
			.ToList();
	}

	// Reads each set's actual "name" (not the filename stem: {slug}-{season} sets like "Clubs 2025-2026" don't match their resource name). JsonDocument avoids a full EloSet round-trip — and the dependency on JsonOpts, which isn't initialized yet when this static field runs.
	static IReadOnlySet<string> LoadBundledEloSetNames()
	{
		var asm = Assembly.GetExecutingAssembly();
		var names = asm.GetManifestResourceNames()
			.Where(n => n.StartsWith(HistoricalEloSetPrefix, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
			.Select(n =>
			{
				using var stream = asm.GetManifestResourceStream(n)!;
				using var doc = JsonDocument.Parse(stream);
				return doc.RootElement.GetProperty("name").GetString() ?? "";
			})
			.Where(name => name.Length > 0)
			.ToHashSet();
		names.Add(CurrentEloSetName);
		return names;
	}

	sealed class CountrySeed
	{
		public string Code3 { get; set; } = "";
		public string Code2 { get; set; } = "";
		public Dictionary<string, string> Name { get; set; } = [];
		public string Confederation { get; set; } = "";
	}

	sealed class ClubSeed
	{
		public string Code { get; set; } = "";
		public Dictionary<string, string> Name { get; set; } = [];
		public string Country { get; set; } = "";
	}
}
