namespace FantasyFootball.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class CompetitionStatisticsPage : ContentPage
{
	public CompetitionStatisticsPage(StandingsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
