using CommunityToolkit.Mvvm.ComponentModel;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /teams/{id} profile page. Display-only — shows the team's elo and rank
/// across every stored <see cref="EloSet"/> that covers it.
/// </summary>
public partial class TeamDetailViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;

	public TeamDetailViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;
	}

	[ObservableProperty]
	public partial Team? Team { get; set; }

	[ObservableProperty]
	public partial IReadOnlyList<EloSetRow> EloSetRows { get; set; } = [];

	public void Load(int teamId)
	{
		Team = _repo.Get<Team>(teamId);
		ReloadRows();
	}

	void ReloadRows()
	{
		if (Team is null) { EloSetRows = []; return; }

		var allSets = _repo.GetAll<EloSet>().Where(s => s.TeamType == Team.Type).ToList();
		var pool = _dataService.AllTeams.Where(t => t.Type == Team.Type).ToList();

		EloSetRows = allSets
			.Select(set =>
			{
				var elo = set.Snapshot.GetValueOrDefault(Team.ShortName);
				var rank = pool.RankByElo(set, Team.Id);
				return new EloSetRow(set.Name, set.Date, elo, rank);
			})
			.Where(r => r.Elo > 0)
			.OrderByDescending(r => r.Date)
			.ToList();
	}
}

public sealed record EloSetRow(string EloSetName, DateOnly Date, int Elo, int Rank);
