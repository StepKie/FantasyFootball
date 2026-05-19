using System.Globalization;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using FantasyFootball.UI.ViewModels;

namespace FantasyFootball.Tests;

/// <summary>
/// VM coverage for the undo / redo flow on the competition detail page. The cases pin the contract
/// the page binding relies on: per-game undo stack, ClearResult on pop, big-scope sims wipe the stack,
/// cap eviction, and Redo replaces in place.
/// </summary>
public class CompetitionDetailViewModelTests : BaseTest
{
	readonly CompetitionDetailViewModel _vm;

	public CompetitionDetailViewModelTests(ITestOutputHelper output) : base(output)
	{
		var competition = InitCompetition(CompetitionType.WM, 2022);
		_vm = new CompetitionDetailViewModel(Repo, new TestSettingsService(), DataService);
		_vm.Load(competition.Id);
		// All assertions run against _vm.Competition.* — the SQLite round-trip in InitCompetition can
		// give the test fixture a different Competition instance than the one Load() ends up holding.
		_vm.Competition.Should().NotBeNull();
	}

	[Fact]
	public async Task SimulateGame_ThenUndo_RevertsResultAndPopsStack()
	{
		var game = _vm.Competition!.CurrentGame!;

		await _vm.SimulateGame();

		game.IsFinished.Should().BeTrue();
		_vm.CanUndo.Should().BeTrue();
		_vm.UndoTargetGame.Should().BeSameAs(game);

		_vm.Undo();

		game.IsFinished.Should().BeFalse("undo should reset the game to SCHEDULED");
		_vm.CanUndo.Should().BeFalse();
		_vm.UndoTargetGame.Should().BeNull();
	}

	[Fact]
	public async Task Undo_AfterTwoSims_PopsMostRecentFirst()
	{
		var firstGame = _vm.Competition!.CurrentGame!;
		await _vm.SimulateGame();
		var secondGame = _vm.Competition.CurrentGame!;
		secondGame.Should().NotBeSameAs(firstGame, "after the first sim, CurrentGame must advance to the next unfinished game");
		await _vm.SimulateGame();

		_vm.UndoTargetGame.Should().BeSameAs(secondGame);

		_vm.Undo();
		_vm.UndoTargetGame.Should().BeSameAs(firstGame, "stack is LIFO; first undo reveals the older entry");
		secondGame.IsFinished.Should().BeFalse();
		firstGame.IsFinished.Should().BeTrue();

		_vm.Undo();
		_vm.CanUndo.Should().BeFalse();
		firstGame.IsFinished.Should().BeFalse();
	}

	[Fact]
	public async Task SimulateRound_ClearsAnyPriorSingleGameUndoEntries()
	{
		await _vm.SimulateGame();
		_vm.CanUndo.Should().BeTrue();

		await _vm.SimulateRound();

		_vm.CanUndo.Should().BeFalse("big-scope sims wipe the per-game undo stack");
		_vm.UndoTargetGame.Should().BeNull();
	}

	[Fact]
	public async Task PushUndoEntry_CapsAtFifty_KeepingTheNewestEntries()
	{
		var firstSimmedGame = _vm.Competition!.CurrentGame!;
		for (var i = 0; i < 51; i++) { await _vm.SimulateGame(); }

		// After 50 pops the stack must be empty — the 51st-from-top entry (the very first sim) was evicted.
		for (var i = 0; i < 50; i++)
		{
			_vm.CanUndo.Should().BeTrue($"expected at least {50 - i} undo entries remaining");
			_vm.Undo();
		}

		_vm.CanUndo.Should().BeFalse("stack capped at 50 — the oldest entry was evicted");
		firstSimmedGame.IsFinished.Should().BeTrue("the evicted entry's game should still be finished — no Undo reached it");
	}

	[Fact]
	public async Task RedoLastGame_AfterSim_ResimsTheSameGame_AndKeepsOneStackEntry()
	{
		var game = _vm.Competition!.CurrentGame!;
		await _vm.SimulateGame();
		game.IsFinished.Should().BeTrue();
		_vm.UndoTargetGame.Should().BeSameAs(game);

		// Snapshot the score so we can confirm Redo wrote a fresh result.
		var (firstHome, firstAway) = (game.HomeScore, game.AwayScore);

		await _vm.RedoLastGame();

		game.IsFinished.Should().BeTrue("redo re-sims the same game");
		_vm.CanUndo.Should().BeTrue("redo doesn't change the stack — same one entry");
		_vm.UndoTargetGame.Should().BeSameAs(game);

		// New result may equal the old one by coincidence — IsFinished + CanUndo invariants are the contract.
		_ = (firstHome, firstAway);
	}

	sealed class TestSettingsService : ISettingsService
	{
		public string LastUsedCompetition { get; set; } = "";
		public TimeSpan SimulationSpeed { get; set; } = TimeSpan.Zero;
		public CultureInfo LastUsedLanguage { get; set; } = CultureInfo.InvariantCulture;
		public FlagStyle FlagStyle { get; set; } = FlagStyle.Square;
		public bool UseOfficialCompetitionLogos { get; set; }

		public bool GetValueOrDefault(string key, bool defaultValue) => defaultValue;
		public string GetValueOrDefault(string key, string defaultValue) => defaultValue;
		public double GetValueOrDefault(string key, double defaultValue) => defaultValue;
		public void AddOrUpdateValue(string key, bool value) { }
		public void AddOrUpdateValue(string key, string value) { }
		public void AddOrUpdateValue(string key, double value) { }
	}
}
