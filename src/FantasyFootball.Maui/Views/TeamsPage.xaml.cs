namespace FantasyFootball.Views;

public partial class TeamsPage : ContentPage
{
	public TeamsPage(TeamsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args) => (BindingContext as TeamsViewModel)?.LoadTeams();
}
