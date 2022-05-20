using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(Type), nameof(Type))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	[ObservableProperty]
	ObservableCollection<Competition> _competitions;

	[ObservableProperty]
	CompetitionType _type;

	CompetitionFactory _competitionFactory;

	public CompetitionsViewModel()
	{
		Title = Type.Name().Short;
		LoadCompetitions();
	}

	void LoadCompetitions()
	{
		Title = Type.Name().Short;
		IsBusy = true;
		Competitions = new(DataStore.GetAll<Competition>().Where(c => c.Type == Type));
		Log.Debug($"Loaded {Competitions.Count} competitions from db ...");
		IsBusy = false;
	}

	[ICommand]
	async Task OpenCompetition(int competitionId)
	{
		var route = $"{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competitionId}";
		await Shell.Current.GoToAsync($"{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competitionId}");
	}

	[ICommand]
	async Task SimulateCompetition()
	{
		Log.Debug("Creating competition");
		_competitionFactory = new EmCompetitionFactory(DataStore);
		var competition = _competitionFactory.Create();
		DataStore.Save(competition);
		Log.Debug("Competition created");
		await OpenCompetition(competition.Id);
	}
}
