namespace FantasyFootball.ViewModels;

public partial class CompetitionSetupViewModel : StandingsViewModel
{
	public List<Group> Groups { get; set; }

	[ICommand]
	async void SelectTeam(TeamRecordViewModel old)
	{
		await Shell.Current.Navigation.PushModalAsync(ServiceHelper.GetService<TeamsPage>());
		// TODO Handle result/listen/pass in object/callback ...
	}
}
