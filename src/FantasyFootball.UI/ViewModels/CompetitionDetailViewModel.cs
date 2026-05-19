using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs /competitions/{id}, the merged Games + Standings page. Loads a
/// Competition by Id, owns the Stage / Round selection + the simulator,
/// and re-publishes change notifications when the simulator fires
/// GameFinishedMessage so the page (which shows games for the current
/// round side-by-side with the standings table) updates as games resolve.
///
/// MAUI splits this into <c>CompetitionDetailViewModel</c> +
/// <c>GamesViewModel</c> + <c>StandingsViewModel</c>; the web port
/// collapses them since both halves render together on one page.
/// </summary>
public partial class CompetitionDetailViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly ISettingsService _settings;
	readonly IDataService _dataService;

	CompetitionSimulator? _simulator;

	public CompetitionDetailViewModel(IRepository repo, ISettingsService settings, IDataService dataService)
	{
		_repo = repo;
		_settings = settings;
		_dataService = dataService;

		MessageBus.Register<GameFinishedMessage>(this, (_, msg) => OnGameFinished(msg.FinishedGame));
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Stages))]
	[NotifyPropertyChangedFor(nameof(Groups))]
	[NotifyPropertyChangedFor(nameof(Winner))]
	[NotifyPropertyChangedFor(nameof(IsFinished))]
	public partial Competition? Competition { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Rounds))]
	public partial Stage? SelectedStage { get; set; }

	[ObservableProperty]
	public partial Round? SelectedRound { get; set; }

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	/// <summary>
	/// Per-session speed override for sim actions. Defaults to the Settings value
	/// on Load; the page's speed control mutates it for the current visit only.
	/// `Instant` short-circuits the inter-game delay and suppresses per-game
	/// re-render messages — a 72-game group stage renders once, not 72 times.
	/// </summary>
	[ObservableProperty]
	public partial SimulationSpeed Speed { get; set; } = SimulationSpeed.Normal;

	public IList<Stage> Stages => Competition?.Stages ?? [];
	public IList<Round> Rounds => SelectedStage?.Rounds ?? [];
	public IList<Group> Groups => Competition?.Groups ?? [];
	public Team? Winner => Competition?.Winner;
	public bool IsFinished => Competition?.IsFinished ?? false;

	public void Load(int competitionId)
	{
		Competition = _repo.Get<Competition>(competitionId);
		if (Competition is null) { return; }

		// Sync the global selected-type so Back-to-Competitions lands on the same
		// category we just left (and the Setup page's pre-filled type matches).
		_dataService.SelectedCompetitionType = Competition.Type;

		SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
		SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();

		// Initial speed = Settings default mapped onto the nearest preset.
		Speed = SimulationSpeedExtensions.FromTimeSpan(_settings.SimulationSpeed);
		_simulator = new CompetitionSimulator(Competition, _repo, (int)_settings.SimulationSpeed.TotalMilliseconds);
		ApplySpeedToSimulator();
	}

	partial void OnSpeedChanged(SimulationSpeed value) => ApplySpeedToSimulator();

	void ApplySpeedToSimulator()
	{
		if (_simulator is null) { return; }
		_simulator.GameDelay = Speed.ToDelay();
		_simulator.Quiet = Speed == SimulationSpeed.Instant;
	}

	public async Task SimulateGame()
	{
		if (_simulator is null || Competition?.CurrentGame is null || IsBusy) { return; }
		IsBusy = true;
		try
		{
			await _simulator.SimulateGame(Competition.CurrentGame);
			_repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}

	public async Task SimulateRound()
	{
		if (_simulator is null || Competition?.CurrentStage?.CurrentRound is null || IsBusy) { return; }
		IsBusy = true;
		try
		{
			await _simulator.SimulateRound(Competition.CurrentStage.CurrentRound);
			_repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}

	public async Task SimulateStage()
	{
		if (_simulator is null || Competition?.CurrentStage is null || IsBusy) { return; }
		IsBusy = true;
		try
		{
			await _simulator.SimulateStage(Competition.CurrentStage);
			_repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}

	public async Task SimulateAll()
	{
		if (_simulator is null || Competition is null || Competition.IsFinished || IsBusy) { return; }
		IsBusy = true;
		try
		{
			await _simulator.Simulate();
			_repo.Save(Competition);
		}
		finally { IsBusy = false; }
	}

	public void Delete()
	{
		if (Competition is null) { return; }
		var deletedId = Competition.Id;
		_repo.Delete(Competition);
		Competition = null;
		MessageBus.Send(new CompetitionDeletedMessage(deletedId));
	}

	void OnGameFinished(Game finished)
	{
		// Bail if the message is for a different competition.
		if (Competition is null || finished.Round?.Stage?.Competition is null) { return; }
		if (finished.Round.Stage.Competition.Id != Competition.Id) { return; }

		// Auto-advance Stage/Round to the next non-finished one so the games
		// pane follows the simulation forward.
		SelectedStage = Competition.CurrentStage ?? Competition.Stages.LastOrDefault();
		SelectedRound = SelectedStage?.CurrentRound ?? SelectedStage?.Rounds.LastOrDefault();

		// Re-publish Competition change so groupings + standings + winner re-evaluate.
		OnPropertyChanged(nameof(Competition));
	}
}
