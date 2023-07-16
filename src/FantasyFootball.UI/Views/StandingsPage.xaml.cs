namespace FantasyFootball.Views;

public partial class StandingsPage : ContentPage
{
	public StandingsPage(StandingsViewModel standingsViewModel)
	{
		InitializeComponent();
		BindingContext = standingsViewModel;
	}
}
