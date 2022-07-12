using System.Reflection;
using CsvHelper;

namespace FantasyFootball.Services;

public class CsvDataService : IDataService
{
	public const string teamsFile = "FantasyFootball.Resources.Data.fifa_elo_new.csv";

	List<Team>? _teamCache;
	readonly IRepository _repo;

	readonly string _languageId;

	public CsvDataService(IRepository repo, CultureInfo? language = null)
	{
		_repo = repo;
		_languageId = language?.TwoLetterISOLanguageName ?? "en";
		Initialize();
		// This class invalidates the cache of AllTeams and forces reload whenever a Team is updated, relieving clients of this necessity
		MessagingCenter.Subscribe<Team>(this, MessageKeys.TeamUpdated, _ => _teamCache = null);
	}

	public void Initialize()
	{
		if (!AllTeams.Any())
		{
			Reset();
		}
	}

	/// <summary> Global selected competition type to sync across all relevant pages </summary>
	public CompetitionType SelectedCompetitionType { get; set; } = CompetitionType.WM;

	/// <summary> Global selected competition year to sync across all relevant pages </summary>
	public int SelectedCompetitionYear { get; set; } = CompetitionType.WM.AvailableYears().Last();

	public List<Team> AllTeams => _teamCache ??= ReloadTeams();

	IList<Country> CreateCountries()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(teamsFile);
		_ = stream ?? throw new FileNotFoundException($"{teamsFile} not found in embedded resource");

		using CsvReader csv = new(new StreamReader(stream), CultureInfo.InvariantCulture);
		_ = csv.Read();
		_ = csv.ReadHeader();

		var countryNameHeaderField = $"country_full_{_languageId}";

		var confederations = _repo.GetAll<Confederation>();
		List<Country> countries = new();

		while (csv.Read())
		{
			var record = new Country
			{
				Name = csv.GetField(countryNameHeaderField),
				Code2 = csv.GetField("country_code_2"),
				Code3 = csv.GetField("country_code_3"),
				Elo = Convert.ToInt32(csv.GetField<double>("elo_new")),
				Confederation = confederations.FirstOrDefault(c => c.Name == csv.GetField("confederation")) ?? Confederation.UNKNOWN,
			};
			countries.Add(record);
		}

		return countries;
	}

	public List<Team> CreateTeams()
	{
		var countries = CreateCountries();
		return countries.Select(country => country.NationalTeam).ToList();
	}

	/// <summary> TODO Check whether the creation of teams/countries can be done more efficiently/if the persistence is safe and not misusable </summary>
	public void Reset()
	{
		_teamCache = null;
		_repo.Reset();
		Confederation.ALL.ForEach(c => _repo.Save(c));
		var teams = CreateTeams();

		foreach (var team in teams)
		{
			_repo.Save(team);
		}

		SelectedCompetitionType = CompetitionType.WM;
		SelectedCompetitionYear = SelectedCompetitionType.AvailableYears().Last();
	}

	List<Team> ReloadTeams()
	{
		var teams = _repo.GetAll<Team>();
		Log.Debug($"Reloaded teams, repo now has {teams.Count} teams");
		return teams;
	}
}
