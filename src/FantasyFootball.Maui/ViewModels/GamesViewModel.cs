namespace FantasyFootball.ViewModels;

public partial class GamesViewModel : CompetitionDetailViewModel
{
	public GamesViewModel()
	{
		MessageBus.Register<GameFinishedMessage>(this, (_, _) => UpdateStageAndRoundFromCompetition());
	}

	public override void LoadCompetition()
	{
		base.LoadCompetition();
		UpdateStageAndRoundFromCompetition();
	}

	void UpdateStageAndRoundFromCompetition()
	{
		SelectedStage = Competition.CurrentStage ?? Competition.Stages.Last();
		SelectedRound = SelectedStage.CurrentRound ?? SelectedStage.Rounds.Last();
	}

	[RelayCommand]
	async Task SimulateGame()
	{
		var game = Competition.CurrentGame;
		if (game == null)
		{
			Log.Debug("Can't simulate: No more games");
			return;
		}

		await Simulator.SimulateGame(game);
		Repo.Save(Competition);
	}

	[RelayCommand]
	async Task SimulateAgain()
	{
		await Shell.Current.GoToAsync("..");
		// await new CompetitionsViewModel(Competition.Type).SimulateCompetitionCommand.ExecuteAsync(false);
	}

	[RelayCommand]
	async Task SimulateCurrentStage()
	{
		_ = Competition.CurrentStage ?? throw new InvalidOperationException($"Can't call {nameof(SimulateCurrentStage)}, {nameof(Competition.CurrentStage)} is null");
		IsBusy = true;
		try
		{
			await Simulator.SimulateStage(Competition.CurrentStage);
			Repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}

	[RelayCommand]
	async Task SimulateCurrentRound()
	{
		_ = Competition.CurrentStage?.CurrentRound ?? throw new InvalidOperationException($"Can't call {nameof(SimulateCurrentRound)}, {nameof(Competition.CurrentStage.CurrentRound)} is null");
		IsBusy = true;
		try
		{
			await Simulator.SimulateRound(Competition.CurrentStage.CurrentRound);
			Repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}
}
