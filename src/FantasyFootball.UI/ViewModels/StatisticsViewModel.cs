using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /statistics page: all-time aggregate records by competition
/// type, computed across every finished competition's games. MAUI sources
/// the same data via StandingsViewModel.OverallRecords; the web port
/// gives it a dedicated VM since the page is standalone.
/// </summary>
public partial class StatisticsViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;

	public StatisticsViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;

		MessageBus.Register<CompetitionFinishedMessage>(this, (_, _) => Reload());

		SelectedCompetitionType = _dataService.SelectedCompetitionType;
		Reload();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	[ObservableProperty]
	public partial CompetitionType SelectedCompetitionType { get; set; }

	[ObservableProperty]
	public partial IList<TeamRecord> OverallRecords { get; set; } = [];

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		_dataService.SelectedCompetitionType = value;
		Reload();
	}

	void Reload()
	{
		var games = _repo.GetAll<Competition>()
			.Where(c => c.IsFinished && c.Type == SelectedCompetitionType)
			.SelectMany(c => c.GamesByDate);
		OverallRecords = Standings.CreateFrom(games);
	}
}
