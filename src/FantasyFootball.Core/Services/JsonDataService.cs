using System.Reflection;
using System.Text.Json;

namespace FantasyFootball.Services;

public class JsonDataService : IDataService
{
	public const string CountriesFile = "FantasyFootball.Resources.Data.countries.json";
	public const string EloCurrentFile = "FantasyFootball.Resources.Data.elo-current.json";

	static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	List<Team>? _teamCache;
	readonly IRepository _repo;
	readonly string _languageId;

	public JsonDataService(IRepository repo, CultureInfo? language = null)
	{
		_repo = repo;
		_languageId = language?.TwoLetterISOLanguageName ?? "en";
		Initialize();
		MessageBus.Register<TeamUpdatedMessage>(this, (_, _) => _teamCache = null);
	}

	public void Initialize()
	{
		if (AllTeams.Count == 0)
		{
			Reset();
		}
	}

	/// <summary> Global selected competition type to sync across all relevant pages </summary>
	public CompetitionType SelectedCompetitionType { get; set; } = CompetitionType.WM;

	public List<Team> AllTeams => _teamCache ??= ReloadTeams();

	List<Country> CreateCountries()
	{
		var seeds = LoadCountrySeeds();
		var elos = LoadCurrentElos();
		var confederations = _repo.GetAll<Confederation>();

		return seeds.Select(s => new Country
		{
			Code2 = s.Code2,
			Code3 = s.Code3,
			Name = s.Name.GetValueOrDefault(_languageId, s.Name["en"]),
			Elo = elos.GetValueOrDefault(s.Code3),
			Confederation = confederations.FirstOrDefault(c => c.Name == s.Confederation) ?? Confederation.UNKNOWN,
		}).ToList();
	}

	public List<Team> CreateTeams()
	{
		var countries = CreateCountries();
		return countries.Select(country => country.NationalTeam).ToList();
	}

	public void Reset()
	{
		_teamCache = null;
		_repo.Reset();
		_repo.SaveAll(Confederation.ALL);
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

	static Dictionary<string, int> LoadCurrentElos()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EloCurrentFile)
			?? throw new FileNotFoundException($"{EloCurrentFile} not found in embedded resources");
		var eloSet = JsonSerializer.Deserialize<EloSet>(stream, JsonOpts)
			?? throw new InvalidOperationException($"{EloCurrentFile} deserialized to null");
		return eloSet.Snapshot;
	}

	sealed class CountrySeed
	{
		public string Code3 { get; set; } = "";
		public string Code2 { get; set; } = "";
		public Dictionary<string, string> Name { get; set; } = [];
		public string Confederation { get; set; } = "";
	}
}
