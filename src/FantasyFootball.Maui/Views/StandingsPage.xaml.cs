namespace FantasyFootball.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class StandingsPage : ContentPage
{
	public StandingsPage(StandingsViewModel standingsViewModel)
	{
		InitializeComponent();
		BindingContext = standingsViewModel;
	}
}
