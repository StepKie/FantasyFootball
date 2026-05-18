namespace FantasyFootball;

public static class Messaging
{
	/// <summary> Global default message bus in the application </summary>
	public static IMessenger MessageBus { get; private set; } = WeakReferenceMessenger.Default;

	// Do not really care to subclass ValueChangedMessage and having to override each constructor ...
	public record CompetitionFinishedMessage(Competition FinishedCompetition);
	public record CompetitionDeletedMessage(int CompetitionId);
	public record GameFinishedMessage(Game FinishedGame);
	public record TeamUpdatedMessage(Team UpdatedTeam);
	/// <summary> Sent after CsvDataService.Reset wipes and re-seeds the data so list VMs can refresh. </summary>
	public record DataResetMessage();

	public static string CompetitionFinished => nameof(CompetitionFinished);
	public static string GameFinished => nameof(GameFinished);
	public static string TeamUpdated => nameof(TeamUpdated);
}
