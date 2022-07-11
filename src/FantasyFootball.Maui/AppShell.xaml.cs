namespace FantasyFootball;

public partial class AppShell : Shell
{

	private bool _isGamesTabVisible = false;

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
		(Shell.Current as AppShell).GamesVisible = visible;
	}
}
