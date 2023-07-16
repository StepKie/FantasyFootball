namespace FantasyFootball.Views;

public partial class CompetitionsPage : ContentPage
{
	public CompetitionsPage(CompetitionsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
