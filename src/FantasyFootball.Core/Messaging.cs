namespace FantasyFootball;

public static class Messaging
{
	/// <summary> Global default message bus in the application </summary>
	public static IMessenger MessageBus { get; private set; } = WeakReferenceMessenger.Default;

	public record TeamUpdatedMessage(Team UpdatedTeam);
	/// <summary> Sent after JsonDataService.Reset wipes and re-seeds the data so list VMs can refresh. </summary>
	public record DataResetMessage();
	/// <summary> Sent when <see cref="IActiveEloSet.Current"/> changes so elo-display VMs can refresh. </summary>
	public record EloSetChangedMessage(EloSet ActiveEloSet);

	public static string TeamUpdated => nameof(TeamUpdated);
}
