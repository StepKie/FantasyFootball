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
	TeamViewModel? _selectedTeam;

	List<TeamViewModel> _allTeams = new();

	public ObservableRangeCollection<TeamViewModel> TeamsInSelectedConfederation { get; set; } = new();

	public IList<string> Confederations { get; } = Confederation.ALL.Select(c => c.Name).Prepend(Res.All).ToList();

	public TeamsViewModel()
	{
		MessageBus.Register<TeamUpdatedMessage>(this, (_, _) => LoadTeams());
		LoadTeams();
	}

	[RelayCommand]
	void LoadTeams()
	{
		IsBusy = true;
		var teamsDb = DataService.AllTeams;
		_allTeams = new(teamsDb.OrderByDescending(t => t.Elo).Select((t, rank) => TeamViewModel.Create(rank + 1, t.Id)));
		IsBusy = false;
		UpdateSelectedTeams();
	}

	partial void OnSelectedConfederationChanged(string value)
	{
		Log.Debug($"Selected confederation changed to {value}");
		UpdateSelectedTeams();
	}

	async partial void OnSelectedTeamChanged(TeamViewModel? value)
	{
		if (value is null) { return; }

		var route = (SelectionType)SelectionMode switch
		{
			SelectionType.SHOW_DETAILS => $"{nameof(TeamDetailPage)}?{nameof(TeamViewModel.TeamId)}={value.TeamId}&{nameof(TeamViewModel.Rank)}={value.Rank}",
			SelectionType.RETURN_ID => $"//{nameof(CompetitionsPage)}/{nameof(CompetitionSetupPage)}?{nameof(CompetitionSetupViewModel.NewTeamIdSelected)}={value.Team.Id}",
			_ => throw new ArgumentOutOfRangeException($"Unexpected SelectionType {SelectionMode}"),
		};

		// Clear selection, reset selection mode and navigate away
		SelectedTeam = null;
		SelectionMode = (int)SelectionType.SHOW_DETAILS;
		await Shell.Current.GoToAsync(route);

	}

	void UpdateSelectedTeams()
	{
		TeamsInSelectedConfederation.ReplaceRange(_allTeams.Where(tvm => SelectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation));
	}

	[RelayCommand]
	Task AddNewTeam() => Shell.Current.DisplayAlert(Res.UnderConstruction, Res.UnderConstructionDetailMsg, "OK"); // Shell.Current.GoToAsync($"{nameof(TeamDetailPage)}");

	[RelayCommand]
	Task OpenSelectedTeam(Team selected) => Shell.Current.GoToAsync($"{nameof(TeamDetailPage)}");
}
