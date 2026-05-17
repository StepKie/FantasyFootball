namespace FantasyFootball.Views;

public partial class CompetitionStatisticsPage : ContentPage
{
	public CompetitionStatisticsPage(StandingsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
