using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Teams page view-model. Lives in the shared UI library so the same instance
/// works in both Blazor WASM and the future MAUI BlazorWebView host.
///
/// Differences from the MAUI VM (FantasyFootball.ViewModels.TeamsViewModel):
/// - No Shell navigation; the .razor page handles row-click navigation via NavigationManager.
/// - No SelectionMode / QueryProperty plumbing; that flow was MAUI-Shell specific and will
///   be replaced by a dialog/route on the web side as the CompetitionSetup port lands.
/// - +new-team is deliberately omitted: the MAUI version is [Obsolete] and shows an
///   "under construction" dialog. Will land as a MudDialog when the feature is built.
/// </summary>
public partial class TeamsViewModel : ObservableObject
{
	readonly IDataService _dataService;

	List<TeamListItem> _allTeams = [];

	public TeamsViewModel(IDataService dataService)
	{
		_dataService = dataService;

		MessageBus.Register<TeamUpdatedMessage>(this, (_, _) => LoadTeams());

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
				.OrderByDescending(t => t.Elo)
				.Select((t, i) => new TeamListItem(i + 1, t))
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
/// from the global Elo ordering; the full TeamViewModel (with editing/save logic)
/// will land with the TeamDetail page port.
/// </summary>
public record TeamListItem(int Rank, Team Team);
