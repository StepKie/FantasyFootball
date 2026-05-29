using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /teams/{id} profile page. Display-only — edit lifecycle lives in
/// EditTeamDialog, which calls back into <see cref="UpdateElo"/> here for the
/// active-EloSet write + broadcast.
/// </summary>
public partial class TeamDetailViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;
	readonly IActiveEloSet _activeEloSet;

	public TeamDetailViewModel(IRepository repo, IDataService dataService, IActiveEloSet activeEloSet)
	{
		_repo = repo;
		_dataService = dataService;
		_activeEloSet = activeEloSet;

		MessageBus.Register<EloSetChangedMessage>(this, (_, _) =>
		{
			if (Team is not null) { ReloadCurrent(); }
		});
	}

	[ObservableProperty]
	public partial Team? Team { get; set; }

	[ObservableProperty]
	public partial int Rank { get; set; }

	[ObservableProperty]
	public partial int Elo { get; set; }

	public void Load(int teamId)
	{
		Team = _repo.Get<Team>(teamId);
		ReloadCurrent();
	}

	void ReloadCurrent()
	{
		if (Team is null) { Rank = 0; Elo = 0; return; }
		Rank = _dataService.AllTeams.RankByElo(_activeEloSet, Team.Id);
		Elo = _activeEloSet.EloOf(Team);
	}

	/// <summary>
	/// Writes a new Elo for the loaded team into the active <see cref="EloSet"/>,
	/// persists the set, and broadcasts the change so list VMs refresh.
	/// </summary>
	public void UpdateElo(int newElo)
	{
		if (Team is null || _activeEloSet.Current is null) { return; }
		if (_activeEloSet.EloOf(Team) == newElo) { return; }

		var current = _activeEloSet.Current;
		var hadKey = current.Snapshot.TryGetValue(Team.ShortName, out var rollback);
		current.Snapshot[Team.ShortName] = newElo;
		try { _repo.Save(current); }
		catch
		{
			if (hadKey) { current.Snapshot[Team.ShortName] = rollback; }
			else { current.Snapshot.Remove(Team.ShortName); }
			throw;
		}
		_activeEloSet.SetCurrent(current);
	}
}
