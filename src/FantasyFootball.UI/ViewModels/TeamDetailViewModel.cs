using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using FantasyFootball.UI.Helpers;
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
    Team = _repo.Get<Team>(teamId);
    Rank = Team is null ? 0 : _dataService.AllTeams.RankByElo(teamId);

    // The repo's in-memory bucket returns the same Team instance across reloads —
    // when triggered by TeamUpdatedMessage after an edit, the reference doesn't
    // change, only the Elo field is mutated in place. [ObservableProperty] skips
    // raising PropertyChanged on ref-equal assignments, so without this nudge
    // Blazor never re-renders the bindings that read Team.Elo.
    OnPropertyChanged(nameof(Team));
  }
}
