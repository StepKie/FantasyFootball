namespace FantasyFootball.Views;

public partial class CompetitionSetupPage : ContentPage
{
	public CompetitionSetupPage(CompetitionSetupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
