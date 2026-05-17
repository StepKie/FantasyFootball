namespace FantasyFootball.Views;

public partial class TeamsPage : ContentPage
{
	public TeamsPage(TeamsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
