namespace FantasyFootball.ViewModels;

public class RecordsGroup : List<TeamRecordViewModel>
{
	public string Name { get; private set; }

	public RecordsGroup(string name, IEnumerable<TeamRecordViewModel> records) : base(records)
	{
		Name = name;
	}
}
