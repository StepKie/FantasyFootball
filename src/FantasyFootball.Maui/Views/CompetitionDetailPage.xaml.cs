namespace FantasyFootball.Views;

public partial class CompetitionDetailPage : ContentPage
{
	public CompetitionDetailPage(CompetitionDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
