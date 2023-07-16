namespace FantasyFootball.ViewModels;

public partial class TeamRecordViewModel : GeneralViewModel
{
	[ObservableProperty] bool _isVisible;

	public TeamRecord Record { get; init; }

	public Color BgColor { get; init; }

	public TeamRecordViewModel(TeamRecord record, Color? bgColor = null, bool isVisible = true)
	{
		Record = record;
		BgColor = bgColor ?? ResourceConstants.DefaultPageColor;
		IsVisible = isVisible;
	}
}
