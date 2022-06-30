using System.Reflection;
using CsvHelper;
using FantasyFootball.Data.CompetitionFactories;

namespace FantasyFootball.Services;

public class CsvDataService : IDataService
{
	public const string teamsFile = "FantasyFootball.Resources.Data.fifa_elo_new.csv";

	readonly IRepository _repo;

	readonly string _languageId;

	public CsvDataService(IRepository repo, CultureInfo? language = null)
	{
		_repo = repo;
		_languageId = language?.TwoLetterISOLanguageName ?? "en";
	}

	/// <summary> Global CompetitionFactory used to setup new Competitions </summary>
	public CompetitionFactory CompetitionFactory { get; set; }

	public IList<Team> AllTeams { get; private set; }

	IList<Country> CreateCountries()
	{
		Confederation.ALL.ForEach(c => _repo.Save(c));

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

	public IList<Team> CreateTeams()
	{
		var countries = CreateCountries();
		return countries.Select(country => country.NationalTeam).ToList();
	}

	/// <summary> TODO Check whether the creation of teams/countries can be done more efficiently/if the persistence is safe and not misusable </summary>
	public void Reset()
	{
		_repo.Reset();
		var teams = CreateTeams();

		foreach (var team in teams)
		{
			_repo.Save(team);
		}

		ReloadTeams();
		CompetitionFactory = EmCompetitionFactory.Default(this);
	}

	void ReloadTeams()
	{
		AllTeams = _repo.GetAll<Team>();
		Log.Debug($"Reloaded teams, repo now has {AllTeams.Count} teams");
	}

	public List<Group> CreateFromHistoricalData(CompetitionType competitionType)
	{
		Dictionary<string, string[]> historicalData = competitionType switch
		{
			CompetitionType.EM => HistoricalData.EM_2020_TEAMS,
			CompetitionType.WM => HistoricalData.WM_2021_TEAMS,
			_ => throw new ArgumentException($"No historical data for {competitionType}"),
		};

		List<Group> groups = new();

		foreach (var entry in historicalData)
		{
			Group group = new() { Name = $"{Res.Group} {entry.Key}" };
			List<Team> teamsInGroup = entry.Value.Select(shortName => Team(shortName)).ToList();
			group.Teams.AddRange(teamsInGroup);
			groups.Add(group);
		}

		return groups;
	}

	public Team Team(string shortName) => AllTeams.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
}
