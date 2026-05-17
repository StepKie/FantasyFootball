namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(TeamId), nameof(TeamId))]
[QueryProperty(nameof(Rank), nameof(Rank))]
public partial class TeamViewModel : GeneralViewModel
{
	List<Team> _teamCache;

	[ObservableProperty]
	public partial string EloString { get; set; }

	[ObservableProperty]
	public partial int Rank { get; set; }

	[ObservableProperty]
	public partial int TeamId { get; set; }

	[ObservableProperty]
	public partial Team Team { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveAndExitCommand))]
	public partial bool TeamWasEdited { get; set; }

	public static TeamViewModel Create(int rank, int teamId) => new() { Rank = rank, TeamId = teamId };

	partial void OnTeamIdChanged(int value)
	{
		_teamCache = DataService.AllTeams;

		Team = Repo.Get<Team>(value)!;
		EloString = Team.Elo.ToString();
		TeamWasEdited = false;
	}

	partial void OnEloStringChanged(string value)
	{
		// All other changes should be in Team via bindings, Elo needs to be parsed
		var canParse = int.TryParse(value, out var elo);

		if (!canParse || elo == Team.Elo)
		{
			return;
		}

		Team.Elo = elo;
		Rank = _teamCache.Count(t => t.Elo > elo) + 1;
	}

	[RelayCommand(CanExecute = nameof(TeamWasEdited))]
	void SaveChanges()
	{
		Repo.Save(Team);
		MessageBus.Send(new TeamUpdatedMessage(Team));
	}

	[RelayCommand(CanExecute = nameof(TeamWasEdited))]
	async Task SaveAndExit()
	{
		SaveChanges();
		_ = await Shell.Current.Navigation.PopAsync();
	}

	[RelayCommand]
	void TrackEditing() => TeamWasEdited = true;
}
