namespace FantasyFootball.ViewModels;

public class RecordsGroup(string name, IEnumerable<TeamRecordViewModel> records) : List<TeamRecordViewModel>(records)
{
	public string Name { get; private set; } = name;
}
