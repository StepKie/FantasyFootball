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

	public ObservableCollection<RoundGroup> GamesByRound { get; private set; }

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
			var loadedCompetitionFromDbById = Repo.Get<Competition>(CompetitionId);
			if (loadedCompetitionFromDbById is null)
			{
				Log.Error($"Unable to find {CompetitionId} in db");
				return;
			}
			Competition = loadedCompetitionFromDbById;
			GamesByRound = new(Competition.Rounds.Select(r => new RoundGroup(r.Name, r.Games.OrderBy(g => g.PlayedOn).Select(g => new GameViewModel(g)))));
			Simulator = new CompetitionSimulator(Competition, Repo);
			Title = $"{Competition.ShortName}-{Competition.Id}";
		}
		catch (Exception e)
		{
			Log.Error($"Failed to load competition: {e}");
		}
	}

	[ICommand]
	async Task DeleteCompetition()
	{
		Repo.Delete(Competition);
		await Shell.Current.GoToAsync(nameof(CompetitionsPage));
	}
}
