namespace FantasyFootball;

public partial class AppShell : Shell
{

	bool _isGamesTabVisible;

	public bool GamesVisible
	{
		get => _isGamesTabVisible;
		set
		{
			_isGamesTabVisible = value;
			OnPropertyChanged();
		}
	}
	public AppShell()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public static void SetGamesVisible(bool visible)
	{
		if (Shell.Current is AppShell shell)
		{
			shell.GamesVisible = visible;
		}
	}
}
