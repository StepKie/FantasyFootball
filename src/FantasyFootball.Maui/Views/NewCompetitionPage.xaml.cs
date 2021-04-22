namespace FantasyFootball.Views;

public partial class NewCompetitionPage : ContentPage
{
	public Competition Competition { get; set; }

	public NewCompetitionPage()
	{
		InitializeComponent();
		BindingContext = new NewCompetitionViewModel();
	}
}
