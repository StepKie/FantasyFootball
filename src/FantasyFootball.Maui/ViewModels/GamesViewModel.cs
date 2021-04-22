using System.Collections.ObjectModel;

namespace FantasyFootball.ViewModels;

public partial class GamesViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Rounds))]
	Stage _selectedStage;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(GamesByRound))]
	Round _selectedRound;

	[ObservableProperty]
	ObservableCollection<RoundGroup> _gamesByRound;

	public Competition Competition { get; init; }

	//public ObservableCollection<GameViewModel> Games { get; set; }

	public GamesViewModel(Competition competition)
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, _ => UpdateStageAndRoundFromCompetition());
		Competition = competition;
		//Games = new(Competition.GamesByDate.Select(g => new GameViewModel(g)));
		GamesByRound = new(Competition.Rounds.Select(r => new RoundGroup(r.Name, r.Games.OrderBy(g => g.PlayedOn).Select(g => new GameViewModel(g)))));
		Stages = Competition.Stages;
		UpdateStageAndRoundFromCompetition();
	}

	public IList<Stage> Stages { get; init; }
	public IList<Round> Rounds => SelectedStage.Rounds;

	void UpdateStageAndRoundFromCompetition()
	{
		SelectedStage = Competition.CurrentStage ?? Competition.Stages.Last();
		SelectedRound = SelectedStage.CurrentRound ?? SelectedStage.Rounds.Last();
	}
}
