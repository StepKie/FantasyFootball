using System.Diagnostics;

namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(CompetitionId), nameof(CompetitionId))]
public partial class CompetitionDetailViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Competition))]
	int _competitionId;

	public Competition Competition => DataStore.Get<Competition>(CompetitionId);

	public GamesViewModel Games => new(Competition);

	public Team? Winner => Competition.LastGame?.Winner;

	public CompetitionSimulator Simulator { get; private set; }

	public string DisplayName => $"{Competition.ShortName} {Competition?.Id}";

	public CompetitionDetailViewModel() { }
	public CompetitionDetailViewModel(int competitionId)
	{
		MessagingCenter.Subscribe<Competition>(this, MessageKeys.CompetitionUpdated, _ => OnPropertyChanged(nameof(Competition)));
		LoadCompetitionId(competitionId);
	}

	void LoadCompetitionId(int competitionId)
	{
		try
		{
			CompetitionId = competitionId;
			Simulator = new CompetitionSimulator(Competition, DataStore);
			Title = $"{Competition.ShortName}-{Competition.Id}";
		}
		catch (Exception e)
		{
			Debug.WriteLine($"Failed to load competition: {e}");
		}
	}

	public IEnumerable<StandingsViewModel> StandingsByGroup => Competition.Groups.Select(group => new StandingsViewModel(group.Name, group.Games));

	[ICommand]
	async Task SimulateGame()
	{
		var game = Competition.CurrentGame;
		if (game == null)
		{
			Log.Debug("Can't simulate: No more games");
			return;
		}

		await Simulator.SimulateGame(game);
	}

	[ICommand]
	async Task SimulateAgain()
	{
		await Shell.Current.GoToAsync("..");
		//await new CompetitionsViewModel(Competition.Type).SimulateCompetitionCommand.ExecuteAsync(false);
	}

	[ICommand]
	async Task SimulateCurrentStage()
	{
		_ = Competition.CurrentStage ?? throw new InvalidOperationException($"Can't call {nameof(SimulateCurrentStage)}, {nameof(Competition.CurrentStage)} is null");
		IsBusy = true;
		await Simulator.SimulateStage(Competition.CurrentStage);
		IsBusy = false;
	}

	[ICommand]
	async Task SimulateCurrentRound()
	{
		_ = Competition.CurrentStage?.CurrentRound ?? throw new InvalidOperationException($"Can't call {nameof(SimulateCurrentRound)}, {nameof(Competition.CurrentStage.CurrentRound)} is null");
		IsBusy = true;
		await Simulator.SimulateRound(Competition.CurrentStage.CurrentRound);
		IsBusy = false;
	}
}
