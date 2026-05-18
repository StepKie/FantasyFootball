using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /teams/{id} profile page. Display-only — edit lifecycle lives in
/// EditTeamDialog, which persists via IRepository and broadcasts
/// TeamUpdatedMessage. This VM subscribes to that message and reloads when the
/// currently-shown team is the one that changed.
///
/// The MAUI equivalent (FantasyFootball.ViewModels.TeamViewModel) doubles as
/// both list-row and edit-page VM; the Blazor port splits those:
///   - TeamListItem (in TeamsViewModel.cs) for list rows
///   - TeamDetailViewModel for the profile page display
///   - EditTeamDialog for the dialog-local edit state
/// </summary>
public partial class TeamDetailViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;

	public TeamDetailViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;

		MessageBus.Register<TeamUpdatedMessage>(this, (_, msg) =>
		{
			if (Team?.Id == msg.UpdatedTeam.Id) { Load(msg.UpdatedTeam.Id); }
		});
	}

	[ObservableProperty]
	public partial Team? Team { get; set; }

	[ObservableProperty]
	public partial int Rank { get; set; }

	public void Load(int teamId)
	{
		var previous = Team;
		Team = _repo.Get<Team>(teamId);
		Rank = Team is null ? 0 : _dataService.AllTeams.RankByElo(teamId);

		// Force notify only on same-ref reload (in-place Elo mutation) — [ObservableProperty]'s setter already fires when the ref changes.
		if (ReferenceEquals(previous, Team)) { OnPropertyChanged(nameof(Team)); }
	}

	// Persists a new Elo for the currently-loaded team. Owns the mutation so the
	// edit dialog doesn't have to touch a [Parameter] object it doesn't own.
	public void UpdateElo(int newElo)
	{
		if (Team is null || newElo == Team.Elo) { return; }

		Team.Elo = newElo;
		_repo.Save(Team);
		MessageBus.Send(new TeamUpdatedMessage(Team));
	}
}
