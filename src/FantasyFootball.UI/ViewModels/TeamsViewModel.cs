using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

public partial class TeamsViewModel : ObservableObject
{
	readonly IDataService _dataService;
	readonly IActiveEloSet _activeEloSet;
	readonly IRepository _repo;

	List<TeamListItem> _allTeams = [];
	bool _suppressActiveChange;

	public TeamsViewModel(IDataService dataService, IActiveEloSet activeEloSet, IRepository repo)
	{
		_dataService = dataService;
		_activeEloSet = activeEloSet;
		_repo = repo;

		MessageBus.Register<EloSetChangedMessage>(this, (_, _) =>
		{
			SyncSelectedFromActive();
			LoadTeams();
		});
		MessageBus.Register<DataResetMessage>(this, (_, _) =>
		{
			LoadEloSets();
			SyncSelectedFromActive();
			LoadTeams();
		});

		Confederations = Confederation.ALL.Select(c => c.Name).Prepend(AllLabel).ToList();
		SelectedConfederation = AllLabel;
		LoadEloSets();
		SyncSelectedFromActive();
		LoadTeams();
	}

	public const string AllLabel = "All";
	public const string CurrentName = "Current";

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

	public bool CanDeleteSelectedSet => SelectedEloSetName is not null and not CurrentName;

	void LoadEloSets()
	{
		var all = _repo.GetAll<EloSet>();
		AvailableEloSetNames = all
			.Select(s => s.Name)
			.OrderBy(name => name == CurrentName ? 0 : 1)
			.ThenBy(name => name, StringComparer.Ordinal)
			.ToList();
	}

	void SyncSelectedFromActive()
	{
		_suppressActiveChange = true;
		try { SelectedEloSetName = _activeEloSet.Current?.Name; }
		finally { _suppressActiveChange = false; }
		OnPropertyChanged(nameof(CanDeleteSelectedSet));
	}

	partial void OnSelectedEloSetNameChanged(string? value)
	{
		if (_suppressActiveChange) { return; }
		if (string.IsNullOrEmpty(value)) { return; }
		if (_activeEloSet.Current?.Name == value) { return; }
		var target = _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == value);
		if (target is not null) { _activeEloSet.SetCurrent(target); }
	}

	/// <summary>Clones the active set's snapshot into a new EloSet with <paramref name="name"/> and makes it active.</summary>
	public void SaveAs(string name)
	{
		var source = _activeEloSet.Current ?? throw new InvalidOperationException("No active EloSet to save.");
		var copy = new EloSet
		{
			Name = name,
			Date = DateOnly.FromDateTime(DateTime.UtcNow),
			Snapshot = new Dictionary<string, int>(source.Snapshot),
		};
		_repo.Save(copy);
		LoadEloSets();
		_activeEloSet.SetCurrent(copy);
	}

	/// <summary>Deletes the active set (unless it's the read-only "Current") and switches the active set back to "Current".</summary>
	public void DeleteSelected()
	{
		if (!CanDeleteSelectedSet) { return; }
		var name = SelectedEloSetName!;
		var target = _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == name);
		if (target is null) { return; }
		_repo.Delete(target);

		var fallback = _repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == CurrentName);
		if (fallback is not null) { _activeEloSet.SetCurrent(fallback); }
		LoadEloSets();
	}

	void LoadTeams()
	{
		IsBusy = true;
		try
		{
			_allTeams = _dataService.AllTeams
				.OrderByDescending(t => _activeEloSet.EloOf(t))
				.Select((t, i) => new TeamListItem(i + 1, t, _activeEloSet.EloOf(t)))
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
/// Lightweight projection for the Teams list. Rank is computed once at load time
/// from the active EloSet's ordering; Elo is snapshotted at load time too so the
/// table row doesn't need to consult IActiveEloSet on every render.
/// </summary>
public record TeamListItem(int Rank, Team Team, int Elo);
