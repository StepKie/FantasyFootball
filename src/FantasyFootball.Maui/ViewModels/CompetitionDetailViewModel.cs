using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(CompetitionId), nameof(CompetitionId))]
public partial class CompetitionDetailViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Competition))]
	int _competitionId;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Stages))]
	[AlsoNotifyChangeFor(nameof(Winner))]
	Competition _competition = new();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(Rounds))]
	Stage? _selectedStage = new();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(GamesByRound))]
	Round? _selectedRound = new();

	public IList<Stage> Stages => Competition.Stages;
	public IList<Round> Rounds => SelectedStage?.Rounds ?? new List<Round>();

	public ObservableCollection<RoundGroup> GamesByRound => new(Competition.Rounds.Select(r => new RoundGroup(r.Name, r.Games.OrderBy(g => g.PlayedOn).Select(g => new GameViewModel(g)))));


	public Team? Winner => Competition.IsFinished ? Competition.LastGame?.Winner : null;

	public CompetitionSimulator Simulator { get; private set; }

	public string DisplayName => $"{Competition.ShortName} {Competition?.Id}";

	public CompetitionDetailViewModel()
	{
		MessagingCenter.Subscribe<Competition>(this, MessageKeys.CompetitionUpdated, _ => OnPropertyChanged(nameof(Competition)));
	}

	partial void OnCompetitionIdChanged(int value) => LoadCompetition();

	public virtual void LoadCompetition()
	{
		try
		{
			var loadedCompetitionFromDbById = DataStore.Get<Competition>(CompetitionId);
			Competition = loadedCompetitionFromDbById;
			Simulator = new CompetitionSimulator(Competition, DataStore);
			Title = $"{Competition.ShortName}-{Competition.Id}";
		}
		catch (Exception e)
		{
			Debug.WriteLine($"Failed to load competition: {e}");
		}
	}

	public IEnumerable<StandingsViewModel> StandingsByGroup => Competition.Groups.Select(group => new StandingsViewModel(group.Name, group.Games));
}
