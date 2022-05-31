using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

public partial class GamesViewModel : CompetitionDetailViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Rounds))]
	Stage _selectedStage = new();

	[ObservableProperty]
	Round _selectedRound = new();

	public IList<Stage> Stages { get; private set; }
	public IList<Round> Rounds => SelectedStage.Rounds;

	public ObservableCollection<RoundGroup> GamesByRound => new(Competition.Rounds.Select(r => new RoundGroup(r.Name, r.Games.OrderBy(g => g.PlayedOn).Select(g => new GameViewModel(g)))));

	//public ObservableCollection<GameViewModel> Games { get; set; }

	public GamesViewModel()
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, _ => UpdateStageAndRoundFromCompetition());
	}

	public override void LoadCompetition()
	{
		base.LoadCompetition();
		//Games = new(Competition.GamesByDate.Select(g => new GameViewModel(g)));
		Stages = Competition.Stages;
		UpdateStageAndRoundFromCompetition();
	}

	void UpdateStageAndRoundFromCompetition()
	{
		SelectedStage = Competition.CurrentStage ?? Competition.Stages.Last();
		SelectedRound = SelectedStage.CurrentRound ?? SelectedStage.Rounds.Last();
	}

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
