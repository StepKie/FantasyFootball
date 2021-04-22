namespace FantasyFootball.ViewModels;

public partial class NewCompetitionViewModel : GeneralViewModel
{
	[ObservableProperty] string _text;
	[ObservableProperty] string _description;

	public NewCompetitionViewModel()
	{
		SaveCommand = new Command(OnSave, ValidateSave);
		CancelCommand = new Command(OnCancel);
		PropertyChanged +=
			(_, __) => SaveCommand.ChangeCanExecute();
	}

	bool ValidateSave() => !string.IsNullOrWhiteSpace(_text) && !string.IsNullOrWhiteSpace(_description);

	public Command SaveCommand { get; }
	public Command CancelCommand { get; }

	// This will pop the current page off the navigation stack
	async void OnCancel() => await Shell.Current.GoToAsync("..");

	async void OnSave()
	{
		Competition competition = new()
		{
			//TODO create this
		};

		DataStore.Save<Competition>(competition);

		// This will pop the current page off the navigation stack
		await Shell.Current.GoToAsync("..");
	}
}
