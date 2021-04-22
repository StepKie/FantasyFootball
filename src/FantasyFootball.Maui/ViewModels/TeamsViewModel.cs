using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

public partial class TeamsViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Teams))]
	string _selectedConfederation;

	[ObservableProperty]
	TeamType _selectedType;

	public ObservableCollection<TeamViewModel> Teams => new(AllTeams.Where(tvm => _selectedConfederation == Res.All || tvm.Team.Country.Confederation.Name == SelectedConfederation));

	public IList<string> Confederations { get; } = Confederation.All.Select(c => c.Name).Prepend(Res.All).ToList();
	public ObservableCollection<TeamViewModel> AllTeams { get; set; }

	public TeamsViewModel()
	{
		MessagingCenter.Subscribe<Team>(this, MessageKeys.RatingChanged, _ => LoadTeams());
		LoadTeams();
		SelectedConfederation = Res.All;
	}

	void LoadTeams()
	{
		var teamsDb = DataStore.GetAll<Team>();
		AllTeams = new(teamsDb.OrderByDescending(t => t.Elo).Select((t, rank) => new TeamViewModel(rank + 1, t)));
	}

	public List<TeamType> Type { get; init; }

	[ICommand]
	Task OpenSelectedTeam(Team selected) => throw new NotImplementedException();
}
