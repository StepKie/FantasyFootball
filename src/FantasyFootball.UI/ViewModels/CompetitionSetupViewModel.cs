using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /competitions/setup page. Owns the lineup-preview state
/// (Original vs Random draw) and exposes two distinct action paths:
///
/// <list type="bullet">
///   <item><b>Single competition</b>: builds one competition from the
///     current lineup, persists it (no auto-sim), navigates to detail
///     for interactive play.</item>
///   <item><b>Bulk simulation</b>: runs the BulkSimRunner across N
///     iterations with a chosen randomness mode (FixedRandom = reuse
///     the current draw / AlwaysRandom = fresh draw per run). Today
///     the run happens inline; chunk C moves it to a background
///     BulkSimSession singleton.</item>
/// </list>
/// </summary>
public partial class CompetitionSetupViewModel : ObservableObject
{
	public enum LineupMode { Original, Random }
	public enum BulkRandomness { Fixed, AlwaysFresh }

	readonly ICompetitionDefinitionStore _definitions;
	readonly IDataService _dataService;
	readonly CompetitionFactory _factory;
	readonly ICompetitionRepository _repo;
	readonly BulkSimRunner _runner;
	readonly IRepository _entityRepo;

	/// <summary>(Type, Year) for every committed definition; flat list to drive the pickers.</summary>
	readonly List<(CompetitionType Type, int Year, string DefinitionId)> _catalog;

	CancellationTokenSource? _cts;
	string[][]? _drawnLineup;

	public CompetitionSetupViewModel(
		ICompetitionDefinitionStore definitions,
		IDataService dataService,
		CompetitionFactory factory,
		ICompetitionRepository repo,
		BulkSimRunner runner,
		IRepository entityRepo)
	{
		_definitions = definitions;
		_dataService = dataService;
		_factory = factory;
		_repo = repo;
		_runner = runner;
		_entityRepo = entityRepo;

		_catalog = _definitions.AvailableIds
			.Select(id => _definitions.Load(id))
			.Select(c => (c.Type, c.Year, c.DefinitionId))
			.OrderBy(x => x.Type)
			.ThenByDescending(x => x.Year)
			.ToList();

		AvailableTypes = _catalog.Select(x => x.Type).Distinct().ToList();

		// Inherit IDataService.SelectedCompetitionType; fall back to catalog's first entry.
		var preferredType = _dataService.SelectedCompetitionType;
		var seed = AvailableTypes.Contains(preferredType)
			? _catalog.First(x => x.Type == preferredType)
			: _catalog.First();
		SelectedType = seed.Type;
		SelectedYear = seed.Year;

		AvailableEloSetNames = LoadEloSetNames();
		SelectedEloSetName = DefaultEloSetNameFor(SelectedYear);

		MessageBus.Register<DataResetMessage>(this, (_, _) => RefreshEloSetNames());
	}

	void RefreshEloSetNames()
	{
		AvailableEloSetNames = LoadEloSetNames();
		if (SelectedEloSetName is not null && !AvailableEloSetNames.Contains(SelectedEloSetName))
		{
			SelectedEloSetName = DefaultEloSetNameFor(SelectedYear);
		}
	}

	public IReadOnlyList<CompetitionType> AvailableTypes { get; }

	public IReadOnlyList<int> Years => _catalog
		.Where(x => x.Type == SelectedType)
		.Select(x => x.Year)
		.OrderByDescending(y => y)
		.ToList();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Years))]
	[NotifyPropertyChangedFor(nameof(Groups))]
	public partial CompetitionType SelectedType { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Groups))]
	public partial int SelectedYear { get; set; }

	[ObservableProperty]
	public partial IReadOnlyList<string> AvailableEloSetNames { get; set; } = [];

	[ObservableProperty]
	public partial string? SelectedEloSetName { get; set; }

	partial void OnSelectedEloSetNameChanged(string? value) =>
		_resolvedEloSet = value is null ? null : _entityRepo.GetAll<EloSet>().FirstOrDefault(s => s.Name == value);

	EloSet? _resolvedEloSet;

	/// <summary>Elo for <paramref name="team"/> from the EloSet currently picked in the dropdown (not the globally active one — the setup page previews under the to-be-applied snapshot).</summary>
	public int EloOf(Team team) => _resolvedEloSet?.Snapshot.GetValueOrDefault(team.ShortName) ?? 0;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Groups))]
	public partial LineupMode CurrentLineup { get; set; } = LineupMode.Original;

	[ObservableProperty]
	public partial BulkRandomness BulkMode { get; set; } = BulkRandomness.Fixed;

	[ObservableProperty]
	public partial int RunCount { get; set; } = 10;

	[ObservableProperty]
	public partial bool IsRunning { get; set; }

	[ObservableProperty]
	public partial int Progress { get; set; }

	[ObservableProperty]
	public partial int? LastRunFirstId { get; set; }

	/// <summary>The lineup currently shown in the preview grid, as Team objects with flags + ELO.</summary>
	public IReadOnlyList<LineupGroup> Groups => BuildGroupsForPreview();

	public string DefinitionId => _catalog
		.FirstOrDefault(x => x.Type == SelectedType && x.Year == SelectedYear)
		.DefinitionId ?? "";

	partial void OnSelectedTypeChanged(CompetitionType value)
	{
		// If the current year isn't available for the new type, snap to the newest year of that type.
		if (!Years.Contains(SelectedYear) && Years.Count > 0)
		{
			SelectedYear = Years[0];
		}

		// Lineup is per-definition; any prior random draw is no longer valid.
		_drawnLineup = null;
		CurrentLineup = LineupMode.Original;

		// EloSet picker is kind-scoped — club elos for club comps, national for nat comps.
		AvailableEloSetNames = LoadEloSetNames();
		SelectedEloSetName = DefaultEloSetNameFor(SelectedYear);

		// Propagate to the shared pref so the list page picks this up on navigate-back.
		_dataService.SelectedCompetitionType = value;
	}

	/// <summary>
	/// Re-reads the shared <see cref="IDataService.SelectedCompetitionType"/>
	/// preference into <see cref="SelectedType"/>. Page calls this in
	/// OnInitialized — the VM is <c>AddScoped</c> so its ctor only fires
	/// once per tab and can't re-seed on later navigations.
	/// </summary>
	public void SyncFromDataService()
	{
		var preferred = _dataService.SelectedCompetitionType;
		if (AvailableTypes.Contains(preferred) && SelectedType != preferred)
		{
			SelectedType = preferred;
		}
	}

	partial void OnSelectedYearChanged(int value)
	{
		_drawnLineup = null;
		CurrentLineup = LineupMode.Original;
		AvailableEloSetNames = LoadEloSetNames();
		SelectedEloSetName = DefaultEloSetNameFor(value);
	}

	// Definition's own EloSetName (e.g. "Bundesliga 2025-2026") if available; else year-matched ("2018"); else "Current". Names come from the live repo so user forks are pickable too.
	string? DefaultEloSetNameFor(int year)
	{
		var defId = _catalog.FirstOrDefault(x => x.Type == SelectedType && x.Year == year).DefinitionId;
		if (defId is not null)
		{
			var declared = _definitions.Load(defId).EloSetName;
			if (declared is not null && AvailableEloSetNames.Contains(declared)) { return declared; }
		}

		var yearName = year.ToString(CultureInfo.InvariantCulture);
		if (AvailableEloSetNames.Contains(yearName)) { return yearName; }

		return AvailableEloSetNames.Contains(JsonDataService.CurrentEloSetName) ? JsonDataService.CurrentEloSetName : AvailableEloSetNames.FirstOrDefault();
	}

	IReadOnlyList<string> LoadEloSetNames()
	{
		var compTeamType = TeamTypeFor(SelectedType);

		return _entityRepo.GetAll<EloSet>()
			.Where(s => s.TeamType == compTeamType)
			.Select(s => s.Name)
			.OrderBy(name => name == JsonDataService.CurrentEloSetName ? 0 : 1)
			.ThenBy(name => name, StringComparer.Ordinal)
			.ToList();
	}

	static TeamType TeamTypeFor(CompetitionType t) => t is CompetitionType.WM or CompetitionType.EM
		? TeamType.NATIONAL_MEN
		: TeamType.CLUB_MEN;

	public void ResetToOriginal()
	{
		_drawnLineup = null;
		CurrentLineup = LineupMode.Original;
	}

	public void RandomDraw()
	{
		var template = _definitions.Load(DefinitionId);
		var groupCount = template.GroupAssignments.Length;
		if (groupCount == 0 || _resolvedEloSet is null) { return; }

		var teamsPerGroup = template.GroupAssignments[0].Length;
		_drawnLineup = new UniformDrawFromRegistry(new EloSetTeamRegistry(_resolvedEloSet)).Draw(groupCount, teamsPerGroup);
		CurrentLineup = LineupMode.Random;
	}

	/// <summary>
	/// Creates a single competition with the current lineup (no auto-sim) and persists it.
	/// Returns the assigned repo Id so the page can navigate to detail.
	/// </summary>
	public async Task<int> CreateSingleAsync()
	{
		var spec = CurrentLineup == LineupMode.Random && _drawnLineup is not null
			? (CompetitionSpec)new CustomLineupSpec { DefinitionId = DefinitionId, Groups = _drawnLineup, EloSetName = SelectedEloSetName }
			: new HistoricalSpec { DefinitionId = DefinitionId, Played = false, EloSetName = SelectedEloSetName };

		var competition = _factory.Create(spec);
		return await _repo.SaveAsync(competition);
	}

	public async Task RunBulkAsync()
	{
		if (IsRunning || RunCount < 1) { return; }

		// Fixed mode reuses the current draw — if none exists yet, draw now.
		if (BulkMode == BulkRandomness.Fixed
			&& CurrentLineup == LineupMode.Random
			&& _drawnLineup is null)
		{
			RandomDraw();
		}

		IsRunning = true;
		Progress = 0;
		LastRunFirstId = null;
		_cts = new CancellationTokenSource();

		try
		{
			CompetitionSpec spec = BuildBulkSpec();
			var progress = new Progress<int>(p => Progress = p);
			var ids = await _runner.RunAsync(spec, RunCount, progress, _cts.Token);
			LastRunFirstId = ids.FirstOrDefault();
		}
		catch (OperationCanceledException)
		{
			// User-initiated; let the page reflect cancelled state.
		}
		finally
		{
			IsRunning = false;
			_cts?.Dispose();
			_cts = null;
		}
	}

	public void Cancel() => _cts?.Cancel();

	CompetitionSpec BuildBulkSpec()
	{
		// AlwaysFresh → RandomLineupSpec; Fixed → CustomLineupSpec (drawn) or HistoricalSpec (original).
		if (BulkMode == BulkRandomness.AlwaysFresh && _resolvedEloSet is not null)
		{
			return new RandomLineupSpec
			{
				DefinitionId = DefinitionId,
				DrawAlgorithm = new UniformDrawFromRegistry(new EloSetTeamRegistry(_resolvedEloSet)),
				EloSetName = SelectedEloSetName,
			};
		}

		return CurrentLineup == LineupMode.Random && _drawnLineup is not null
			? new CustomLineupSpec { DefinitionId = DefinitionId, Groups = _drawnLineup, EloSetName = SelectedEloSetName }
			: new HistoricalSpec { DefinitionId = DefinitionId, Played = false, EloSetName = SelectedEloSetName };
	}

	IReadOnlyList<LineupGroup> BuildGroupsForPreview()
	{
		if (string.IsNullOrEmpty(DefinitionId)) { return []; }

		var template = _definitions.Load(DefinitionId);
		// Codes collide across kinds (STP = São Tomé and Príncipe national + FC St. Pauli club). Pick the right pool by competition type.
		var pool = IsNationalComp(template.Type)
			? _dataService.AllTeams.Where(t => t.IsNationalTeam)
			: _dataService.AllTeams.Where(t => !t.IsNationalTeam);
		var teams = pool.ToDictionary(t => t.ShortName, t => t);

		if (template.IsLeague())
		{
			var rows = template.Teams
				.Select(id => teams.TryGetValue(id, out var t) ? t : Placeholder(id))
				.ToList();

			return [new LineupGroup(template.Title, rows)];
		}

		var lineup = CurrentLineup == LineupMode.Random && _drawnLineup is not null
			? _drawnLineup
			: template.GroupAssignments;

		var result = new List<LineupGroup>(lineup.Length);
		for (int i = 0; i < lineup.Length; i++)
		{
			var letter = ((char)('A' + i)).ToString();
			var rows = lineup[i]
				.Select(id => teams.TryGetValue(id, out var t) ? t : Placeholder(id))
				.ToList();
			result.Add(new LineupGroup($"Group {letter}", rows));
		}

		return result;
	}

	public bool IsLeagueDefinition => !string.IsNullOrEmpty(DefinitionId) && _definitions.Load(DefinitionId).IsLeague();

	// Placeholder Team for IDs not in the local registry (e.g. synthetic test IDs from an extended-pool draw).
	static Team Placeholder(string shortName) => new() { Name = shortName, ShortName = shortName };

	static bool IsNationalComp(CompetitionType t) => t is CompetitionType.WM or CompetitionType.EM;

	public sealed record LineupGroup(string Name, IReadOnlyList<Team> Teams);
}
