using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

public partial class TeamsViewModel : ObservableObject
{
	readonly IDataService _dataService;
	readonly IActiveEloSet _activeEloSet;

	List<TeamListItem> _allTeams = [];

	public TeamsViewModel(IDataService dataService, IActiveEloSet activeEloSet)
	{
		_dataService = dataService;
		_activeEloSet = activeEloSet;

		MessageBus.Register<EloSetChangedMessage>(this, (_, _) => LoadTeams());
		MessageBus.Register<DataResetMessage>(this, (_, _) => LoadTeams());

		Confederations = Confederation.ALL.Select(c => c.Name).Prepend(AllLabel).ToList();
		SelectedConfederation = AllLabel;
		LoadTeams();
	}

	// "All" sentinel used to show every confederation. AppResources.All exists but the
	// resource manager isn't reliably initialized in Blazor WASM without extra wiring;
	// a fixed English string is fine here until the i18n follow-up lands.
	public const string AllLabel = "All";

	public IList<string> Confederations { get; }

	[ObservableProperty]
	public partial string SelectedConfederation { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<TeamListItem> TeamsInSelectedConfederation { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

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
