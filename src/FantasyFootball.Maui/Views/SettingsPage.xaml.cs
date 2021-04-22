namespace FantasyFootball.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel viewmodel)
	{
		InitializeComponent();
		BindingContext = viewmodel;
	}
}
