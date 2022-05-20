using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

public partial class TeamsViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Teams))]
	string _selectedConfederation;

	[ObservableProperty]
	TeamType _selectedType;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Teams))]
	ObservableCollection<TeamViewModel> _allTeams = new();

	public ObservableCollection<TeamViewModel> Teams => new(AllTeams.Where(tvm => _selectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation));

	public IList<string> Confederations { get; } = Confederation.All.Select(c => c.Name).Prepend(Res.All).ToList();

	public TeamsViewModel()
	{
		MessagingCenter.Subscribe<Team>(this, MessageKeys.RatingChanged, _ => LoadTeams());
		LoadTeams();
		SelectedConfederation = Res.All;
	}

	[ICommand]
	async void LoadTeams()
	{
		IsBusy = true;
		var teamsDb = await DataStore.GetAllAsync<Team>();
		AllTeams = new(teamsDb.OrderByDescending(t => t.Elo).Select((t, rank) => new TeamViewModel(rank + 1, t)));
		IsBusy = false;
	}

	public List<TeamType> Type { get; init; }

	[ICommand]
	Task OpenSelectedTeam(Team selected) => throw new NotImplementedException();
}
