using System.Windows.Input;

namespace FantasyFootball.ViewModels;

public partial class TeamViewModel : GeneralViewModel
{
	[ObservableProperty]
	string _eloString;

	public Team Team { get; init; }
	public int Rank { get; init; }

	public TeamViewModel(int rank, Team team)
	{
		Team = team;
		EloString = Team.Elo.ToString();
		Rank = rank;
	}

	[ICommand]
	void SaveEloChange()
	{
		var canParse = int.TryParse(EloString, out var elo);
		if (!canParse || elo == Team.Elo)
		{
			return;
		}
		Team.Elo = elo;
		Repo.Save(Team);
		MessagingCenter.Send(Team, MessageKeys.RatingChanged);
	}
}
