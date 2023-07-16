namespace FantasyFootball.Views;

public partial class TeamDetailPage : ContentPage
{
	public TeamDetailPage(TeamViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
