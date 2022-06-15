namespace FantasyFootball.ViewModels;

public partial class TeamsViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(TestLabel2))]
	string _testLabel1;

	public string TestLabel2 => _testLabel1 + "Dep";

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Teams))]
	string _selectedConfederation = Res.All;

	[ObservableProperty]
	TeamType _selectedType;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Teams))]
	ObservableCollection<TeamViewModel> _allTeams = new();

	public List<TeamViewModel> Teams => AllTeams.Where(tvm => SelectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation).ToList();

	public IList<string> Confederations { get; } = Confederation.ALL.Select(c => c.Name).Prepend(Res.All).ToList();

	public TeamsViewModel()
	{
		MessagingCenter.Subscribe<Team>(this, MessageKeys.RatingChanged, _ => LoadTeams());
		//LoadTeams();
	}

	[ICommand]
	public async void LoadTeams()
	{
		IsBusy = true;
		var teamsDb = await DataStore.GetAllAsync<Team>();
		AllTeams = new(teamsDb.OrderByDescending(t => t.Elo).Select((t, rank) => new TeamViewModel(rank + 1, t)));
		SelectedConfederation = Confederation.AFC.Name;
		TestLabel1 = "Set TestLabel1";
		IsBusy = false;
		// TODO Figure out why this is necessary, AllTeams should notify Teams! Maybe try it with a string label, might have to do with creating an ObservableCollection each time?
		//OnPropertyChanged(nameof(Teams));
	}

	public List<TeamType> Type { get; init; }

	[ICommand]
	Task OpenSelectedTeam(Team selected) => throw new NotImplementedException();
}
