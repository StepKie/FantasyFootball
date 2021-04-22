using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(Type), nameof(Type))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	[ObservableProperty]
	ObservableCollection<CompetitionDetailViewModel> _competitionVms;

	[ObservableProperty]
	CompetitionType _type;

	CompetitionFactory _competitionFactory;

	public CompetitionsViewModel()
	{
		Title = Type.Name().Short;
		ReloadCompetitions();
	}

	void ReloadCompetitions()
	{
		Title = Type.Name().Short;
		IsBusy = true;
		CompetitionVms = new(DataStore.GetAll<Competition>().Where(c => c.Type == Type).Select(competition => new CompetitionDetailViewModel(competition.Id)));
		Log.Debug($"Reloaded {CompetitionVms.Count} competitions from db ...");
		IsBusy = false;
	}

	[ICommand]
	async Task OpenCompetition(int competitionId) => await Shell.Current.GoToAsync($"{nameof(CompetitionDetailPage)}?{nameof(CompetitionDetailViewModel.CompetitionId)}={competitionId}");

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
