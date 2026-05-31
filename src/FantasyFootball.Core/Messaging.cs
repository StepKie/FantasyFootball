namespace FantasyFootball;

public static class Messaging
{
	/// <summary> Global default message bus in the application </summary>
	public static IMessenger MessageBus { get; private set; } = WeakReferenceMessenger.Default;

	/// <summary> Sent after JsonDataService.Reset wipes and re-seeds the data so list VMs can refresh. </summary>
	public record DataResetMessage();
}
