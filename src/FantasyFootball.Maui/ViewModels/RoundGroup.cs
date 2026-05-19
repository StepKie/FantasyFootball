namespace FantasyFootball.ViewModels;

public class RoundGroup(string name, IEnumerable<GameViewModel> games) : List<GameViewModel>(games)
{
	public string Name { get; private set; } = name;
}
