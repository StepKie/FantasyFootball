using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

public partial class TeamsViewModel : ObservableObject
{
	readonly IDataService _dataService;
	readonly IRepository _repo;

	List<TeamListItem> _allTeams = [];

	public TeamsViewModel(IDataService dataService, IRepository repo)
	{
		_dataService = dataService;
		_repo = repo;

		MessageBus.Register<DataResetMessage>(this, (_, _) =>
		{
			LoadEloSets();
			LoadTeams();
		});

		Confederations = Confederation.ALL.Select(c => c.Name).Prepend(AllLabel).ToList();
		SelectedConfederation = AllLabel;
		LoadEloSets();
		LoadTeams();
	}

	public const string AllLabel = "All";

	public IList<string> Confederations { get; }

	[ObservableProperty]
	public partial string SelectedConfederation { get; set; }

	[ObservableProperty]
	public partial IReadOnlyList<string> AvailableEloSetNames { get; set; } = [];

	[ObservableProperty]
	public partial string? SelectedEloSetName { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<TeamListItem> TeamsInSelectedConfederation { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	// Bundled EloSets are read-only. SaveAs / Delete are off the table for now (re-introduce when the per-set editor lands).
	public bool CanDeleteSelectedSet =>
		SelectedEloSetName is not null
		&& !JsonDataService.BundledEloSetNames.Contains(SelectedEloSetName);

	void LoadEloSets()
	{
		AvailableEloSetNames = _repo.GetAll<EloSet>()
			.Select(s => s.Name)
			.OrderBy(name => name == JsonDataService.CurrentEloSetName ? 0 : 1)
			.ThenBy(name => name, StringComparer.Ordinal)
			.ToList();

		SelectedEloSetName ??= AvailableEloSetNames.FirstOrDefault();
	}

	partial void OnSelectedEloSetNameChanged(string? value) => LoadTeams();

	void LoadTeams()
	{
		IsBusy = true;
		try
		{
			var set = SelectedEloSetName is null
				? null
				: _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == SelectedEloSetName);
			if (set is null) { _allTeams = []; UpdateFilteredTeams(); return; }

			_allTeams = _dataService.AllTeams
				.Where(t => t.Type == set.TeamType)
				.OrderByDescending(t => set.Snapshot.GetValueOrDefault(t.ShortName))
				.Select((t, i) => new TeamListItem(i + 1, t, set.Snapshot.GetValueOrDefault(t.ShortName)))
				.ToList();
			UpdateFilteredTeams();
		}
		finally
		{
			IsBusy = false;
		}
	}

	partial void OnSelectedConfederationChanged(string value) => UpdateFilteredTeams();

	void UpdateFilteredTeams()
	{
		var filtered = _allTeams.Where(item =>
			SelectedConfederation == AllLabel ||
			item.Team.Country.Confederation.Name == SelectedConfederation);

		TeamsInSelectedConfederation = new ObservableCollection<TeamListItem>(filtered);
	}
}

/// <summary>
/// Lightweight projection for the Teams list. Rank is computed once at load time from
/// the selected EloSet's ordering; Elo is snapshotted at load time too.
/// </summary>
public record TeamListItem(int Rank, Team Team, int Elo);
