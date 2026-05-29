namespace FantasyFootball.Services;

public sealed class ActiveEloSet : IActiveEloSet
{
	public EloSet? Current { get; private set; }

	public void SetCurrent(EloSet eloSet)
	{
		Current = eloSet;
		MessageBus.Send(new EloSetChangedMessage(eloSet));
	}
}
