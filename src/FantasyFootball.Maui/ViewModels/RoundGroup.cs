namespace FantasyFootball.ViewModels;

public class RoundGroup : List<GameViewModel>
{
	public string Name { get; private set; }

	public RoundGroup(string name, IEnumerable<GameViewModel> games) : base(games)
	{
		Name = name;
	}
}
