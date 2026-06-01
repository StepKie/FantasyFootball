using CommunityToolkit.Mvvm.ComponentModel;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /competitions list page. Loads from
/// <see cref="ICompetitionRepository"/>, filters by
/// <see cref="CompetitionType"/>, sorts newest-first.
///
/// Deferred from the old VM: active/finished tabs (status shown inline
/// as a chip per row instead), TeamRecord aggregate (lands with the
/// aggregator chunk), MessageBus integration (reload-on-mount covers
/// the current navigation patterns).
/// </summary>
public partial class CompetitionsViewModel : ObservableObject
{
	readonly ICompetitionRepository _repo;
	readonly IDataService _dataService;
	readonly CompetitionFactory _factory;
	readonly CompetitionSimulator _simulator;
	readonly IRepository _entityRepo;

	public CompetitionsViewModel(
		ICompetitionRepository repo,
		IDataService dataService,
		CompetitionFactory factory,
		CompetitionSimulator simulator,
		IRepository entityRepo)
	{
		_repo = repo;
		_dataService = dataService;
		_factory = factory;
		_simulator = simulator;
		_entityRepo = entityRepo;

		// Hydrate from the shared type pref so the chip state survives navigation between list and setup pages.
		SelectedType = dataService.SelectedCompetitionType;
	}

	partial void OnSelectedTypeChanged(CompetitionType? value)
	{
		// Persist non-null choices so other consumers inherit the pick; null = "All" filter, leave the prior pref alone.
		if (value is { } t) { _dataService.SelectedCompetitionType = t; }
	}

	/// <summary>
	/// Re-reads the shared <see cref="IDataService.SelectedCompetitionType"/>
	/// preference into <see cref="SelectedType"/>. Pages call this in
	/// OnInitialized so the chip state survives navigation — the VM itself
	/// is registered <c>AddScoped</c>, so its constructor only runs once
	/// per tab and can't re-seed.
	/// </summary>
	public void SyncFromDataService()
	{
		var preferred = _dataService.SelectedCompetitionType;
		if (SelectedType != preferred) { SelectedType = preferred; }
	}

	public IReadOnlyList<CompetitionType?> AvailableTypeFilters { get; } =
		[null, CompetitionType.WM, CompetitionType.EM, CompetitionType.DOMESTIC_LEAGUE];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(FilteredCompetitions))]
	public partial CompetitionType? SelectedType { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(FilteredCompetitions))]
	public partial IReadOnlyList<Competition> AllCompetitions { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	public IReadOnlyList<Competition> FilteredCompetitions => SelectedType is { } t
		? AllCompetitions.Where(c => c.Type == t).ToList()
		: AllCompetitions;

	/// <summary>
	/// Team-level aggregate across every finished competition currently
	/// visible (after the type filter). Played games are summed into a
	/// single row per team. Sorted by points desc → GD desc → GF desc.
	/// </summary>
	public IReadOnlyList<TeamRecord> OverallRecords
	{
		get
		{
			// Codes collide across kinds (STP = São Tomé and Príncipe national + FC St. Pauli club). Aggregate per (isNational, code).
			var clubsByCode = _dataService.AllTeams.Where(t => !t.IsNationalTeam).ToDictionary(t => t.ShortName, t => t);
			var nationsByCode = _dataService.AllTeams.Where(t => t.IsNationalTeam).ToDictionary(t => t.ShortName, t => t);
			var perTeam = new Dictionary<(bool IsNational, string Code), TeamRecord.Mutable>();

			foreach (var comp in FilteredCompetitions.Where(c => c.IsFinished()))
			{
				var isNational = IsNationalComp(comp.Type);
				var champion = comp.WinnerTeamId();
				foreach (var game in comp.Games)
				{
					if (game.Result is not { } r) { continue; }

					var home = game.HomeTeamId;
					var away = game.AwayTeamId;
					if (home is null || away is null) { continue; }

					Accumulate(perTeam, (isNational, home), r, isHomeTeam: true);
					Accumulate(perTeam, (isNational, away), r, isHomeTeam: false);
				}

				if (champion is not null)
				{
					var key = (isNational, champion);
					var entry = perTeam.GetValueOrDefault(key);
					perTeam[key] = entry with { CompetitionWins = entry.CompetitionWins + 1 };
				}
			}

			return perTeam
				.Select(kv =>
				{
					var lookup = kv.Key.IsNational ? nationsByCode : clubsByCode;
					var team = lookup.TryGetValue(kv.Key.Code, out var t) ? t : new Team { Name = kv.Key.Code, ShortName = kv.Key.Code };

					return new TeamRecord(team,
						kv.Value.Wins, kv.Value.Draws, kv.Value.Losses,
						kv.Value.GoalsFor, kv.Value.GoalsAgainst,
						kv.Value.CompetitionWins);
				})
				.OrderByDescending(r => r.Points)
				.ThenByDescending(r => r.GoalDifference)
				.ThenByDescending(r => r.GoalsFor)
				.ThenBy(r => r.Team.ShortName, StringComparer.Ordinal)
				.ToList();
		}
	}

	static bool IsNationalComp(CompetitionType t) => t is CompetitionType.WM or CompetitionType.EM;

	static void Accumulate(Dictionary<(bool IsNational, string Code), TeamRecord.Mutable> store, (bool IsNational, string Code) key, Result r, bool isHomeTeam)
	{
		var scored = isHomeTeam ? r.HomeScore : r.AwayScore;
		var conceded = isHomeTeam ? r.AwayScore : r.HomeScore;
		var won = isHomeTeam ? r.HomeWon : r.AwayWon;
		var lost = isHomeTeam ? r.AwayWon : r.HomeWon;
		var row = store.GetValueOrDefault(key);
		store[key] = row with
		{
			GoalsFor = row.GoalsFor + scored,
			GoalsAgainst = row.GoalsAgainst + conceded,
			Wins = row.Wins + (won ? 1 : 0),
			Losses = row.Losses + (lost ? 1 : 0),
			Draws = row.Draws + (!won && !lost ? 1 : 0),
		};
	}

	/// <summary>
	/// Re-reads the repo into the visible list. Bundled historicals are user-imported
	/// via Settings → "Restore historical competitions"; this VM never auto-seeds.
	/// </summary>
	public Task InitializeAsync() => ReloadAsync();

	public async Task ReloadAsync()
	{
		IsBusy = true;
		try
		{
			var all = await _repo.GetAllAsync();
			AllCompetitions = all.OrderByDescending(c => c.Id).ToList();
		}
		finally
		{
			IsBusy = false;
		}
	}

	public async Task DeleteAsync(int id)
	{
		await _repo.DeleteAsync(id);
		await ReloadAsync();
	}

	public async Task DeleteAllAsync()
	{
		await _repo.ResetAsync();
		await ReloadAsync();
	}

	/// <summary>
	/// Fast-forward an unfinished competition to completion: load, sim every
	/// remaining game, save, refresh the list. Used by the list-row FF button
	/// so users can finalise an in-progress comp without opening it.
	/// </summary>
	public async Task FastForwardAsync(int id)
	{
		if (IsBusy) { return; }
		var comp = await _repo.GetAsync(id);
		if (comp is null || comp.IsFinished()) { return; }
		IsBusy = true;
		try
		{
			// Yield so the IsBusy spinner flushes before the synchronous sim hogs the WASM thread.
			await Task.Yield();
			var scoreModel = HistoricalScoreModelResolver.Resolve(comp, _entityRepo)
				?? throw new InvalidOperationException($"Cannot resolve EloSet '{comp.EloSetName}' for competition '{comp.DefinitionId}'.");
			_simulator.Simulate(comp, scoreModel);
			await _repo.SaveAsync(comp);
			await ReloadAsync();
		}
		finally
		{
			IsBusy = false;
		}
	}

	/// <summary>
	/// Clones a finished competition's setup into a new scheduled one and persists.
	/// Same Type+Year+lineup, no auto-sim. Returns the new id so the caller can navigate.
	/// </summary>
	public async Task<int?> ReplayAsync(int sourceId)
	{
		var source = await _repo.GetAsync(sourceId);
		if (source is null) { return null; }

		// Leagues have a fixed schedule — no lineup to substitute. Replay = re-run the historical fixture list, results cleared.
		CompetitionSpec spec = source.IsLeague()
			? new HistoricalSpec { DefinitionId = source.DefinitionId, Played = false, EloSetName = source.EloSetName }
			: new CustomLineupSpec
			{
				DefinitionId = source.DefinitionId,
				Groups = source.GroupAssignments.Select(g => (string[])g.Clone()).ToArray(),
				EloSetName = source.EloSetName,
			};
		var replay = _factory.Create(spec);
		var id = await _repo.SaveAsync(replay);
		await ReloadAsync();
		return id;
	}
}

/// <summary>
/// All-time team aggregate row for the Overall Standings table. Built
/// across every finished <see cref="Competition"/> the user can see.
/// </summary>
public sealed record TeamRecord(
	Team Team,
	int Wins,
	int Draws,
	int Losses,
	int GoalsFor,
	int GoalsAgainst,
	int CompetitionWins)
{
	public int Points => 3 * Wins + Draws;
	public int GoalDifference => GoalsFor - GoalsAgainst;
	public int MatchesPlayed => Wins + Draws + Losses;

	internal record struct Mutable(int Wins, int Draws, int Losses, int GoalsFor, int GoalsAgainst, int CompetitionWins);
}
