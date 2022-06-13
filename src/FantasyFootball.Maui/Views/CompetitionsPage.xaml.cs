namespace FantasyFootball.Views;

public partial class CompetitionsPage : ContentPage
{
	public CompetitionsPage(CompetitionsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args) => (BindingContext as CompetitionsViewModel)?.ReloadCompetitions();
}
