using Xamarin.CommunityToolkit.ObjectModel;

namespace FantasyFootball.ViewModels;

public partial class TeamsViewModel : GeneralViewModel
{
	[ObservableProperty]
	string _selectedConfederation = Res.All;

	/// <summary> Currently unused </summary>
	[ObservableProperty]
	TeamType _selectedType;

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

	void UpdateSelectedTeams()
	{
		TeamsInSelectedConfederation.ReplaceRange(_allTeams.Where(tvm => SelectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation));
	}

	[ICommand]
	Task OpenSelectedTeam(Team selected) => throw new NotImplementedException();
}
