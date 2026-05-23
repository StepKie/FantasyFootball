namespace FantasyFootball;

public static class Messaging
{
	/// <summary> Global default message bus in the application </summary>
	public static IMessenger MessageBus { get; private set; } = WeakReferenceMessenger.Default;

	public record CompetitionDeletedMessage(int CompetitionId);
	public record TeamUpdatedMessage(Team UpdatedTeam);
	/// <summary> Sent after CsvDataService.Reset wipes and re-seeds the data so list VMs can refresh. </summary>
	public record DataResetMessage();

	public static string TeamUpdated => nameof(TeamUpdated);
}
