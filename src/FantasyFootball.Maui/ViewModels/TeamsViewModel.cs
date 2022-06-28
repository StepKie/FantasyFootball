using Xamarin.CommunityToolkit.ObjectModel;

namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectionMode), nameof(SelectionMode))]
[QueryProperty(nameof(SelectedConfederation), nameof(SelectedConfederation))]
public partial class TeamsViewModel : GeneralViewModel
{
	[ObservableProperty]
	string _selectedConfederation = Res.All;

	[ObservableProperty]
	int _selectionMode;

	/// <summary> Currently unused </summary>
	[ObservableProperty]
	TeamType _selectedType;

	[ObservableProperty]
	TeamViewModel _selectedTeam;

	List<TeamViewModel> _allTeams = new();

	public ObservableRangeCollection<TeamViewModel> TeamsInSelectedConfederation { get; set; } = new();

	public IList<string> Confederations { get; } = Confederation.ALL.Select(c => c.Name).Prepend(Res.All).ToList();

	public TeamsViewModel()
	{
		MessagingCenter.Subscribe<Team>(this, MessageKeys.RatingChanged, _ => LoadTeams());
	}

	[ICommand]
	public async void LoadTeams()
	{
		IsBusy = true;
		var teamsDb = await Repo.GetAllAsync<Team>();
		_allTeams = new(teamsDb.OrderByDescending(t => t.Elo).Select((t, rank) => new TeamViewModel(rank + 1, t)));
		IsBusy = false;
		UpdateSelectedTeams();
	}

	partial void OnSelectedConfederationChanged(string value)
	{
		Log.Debug($"Selected confederation changed to {value}");
		UpdateSelectedTeams();
	}

	partial void OnSelectedTeamChanged(TeamViewModel value)
	{
		var route = (SelectionType)SelectionMode switch
		{
			SelectionType.SHOW_DETAILS => $"",
			SelectionType.RETURN_ID => $"//{nameof(CompetitionsPage)}/{nameof(CompetitionSetupPage)}?{nameof(CompetitionSetupViewModel.NewTeamIdSelected)}={value.Team.Id}",
			_ => throw new ArgumentOutOfRangeException($"Unexpected SelectionType {SelectionMode}"),
		};

		Shell.Current.GoToAsync(route);

	}

	void UpdateSelectedTeams()
	{
		TeamsInSelectedConfederation.ReplaceRange(_allTeams.Where(tvm => SelectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation));
	}

	[ICommand]
	Task OpenSelectedTeam(Team selected) => throw new NotImplementedException();
}
