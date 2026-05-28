using System.Reflection;
using System.Text.Json;

namespace FantasyFootball.Services;

public class JsonDataService : IDataService
{
	public const string CountriesFile = "FantasyFootball.Resources.Data.countries.json";
	public const string EloCurrentFile = "FantasyFootball.Resources.Data.elo-current.json";

	public const string CurrentEloSetName = "Current";

	static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	List<Team>? _teamCache;
	readonly IRepository _repo;
	readonly IActiveEloSet _activeEloSet;
	readonly string _languageId;

	public JsonDataService(IRepository repo, IActiveEloSet activeEloSet, CultureInfo? language = null)
	{
		_repo = repo;
		_activeEloSet = activeEloSet;
		_languageId = language?.TwoLetterISOLanguageName ?? "en";
		Initialize();
	}

	public void Initialize()
	{
		if (AllTeams.Count == 0) { Reset(); return; }

		// Repo already populated from a previous session — ensure the active set is also wired up.
		var current = _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == CurrentEloSetName);
		if (current is not null) { _activeEloSet.SetCurrent(current); }
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
			Name = s.Name.GetValueOrDefault(_languageId, s.Name["en"]),
			Confederation = confederations.FirstOrDefault(c => c.Name == s.Confederation) ?? Confederation.UNKNOWN,
		}).ToList();
	}

	public List<Team> CreateTeams() => CreateCountries().Select(country => country.NationalTeam).ToList();

	public void Reset()
	{
		_teamCache = null;
		_repo.Reset();
		_repo.SaveAll(Confederation.ALL);

		var currentEloSet = LoadCurrentEloSet();
		_repo.Save(currentEloSet);
		_activeEloSet.SetCurrent(currentEloSet);

		_repo.SaveAll(CreateTeams());

		SelectedCompetitionType = CompetitionType.WM;

		MessageBus.Send(new DataResetMessage());
	}

	List<Team> ReloadTeams()
	{
		var teams = _repo.GetAll<Team>();
		Log.Debug($"Reloaded teams, repo now has {teams.Count} teams");
		return teams;
	}

	static List<CountrySeed> LoadCountrySeeds()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CountriesFile)
			?? throw new FileNotFoundException($"{CountriesFile} not found in embedded resources");
		return JsonSerializer.Deserialize<List<CountrySeed>>(stream, JsonOpts)
			?? throw new InvalidOperationException($"{CountriesFile} deserialized to null");
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

	sealed class CountrySeed
	{
		public string Code3 { get; set; } = "";
		public string Code2 { get; set; } = "";
		public Dictionary<string, string> Name { get; set; } = [];
		public string Confederation { get; set; } = "";
	}
}
