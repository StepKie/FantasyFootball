namespace FantasyFootball.ViewModels;

/// <summary>
/// Nicht domänenspezifische Funktionalität
/// zusätzlich zu den sehr sinnvollen Funktionalitäten in ObservableObject
/// </summary>
public abstract partial class GeneralViewModel : ObservableObject
{
	// Different dependency resolution than dependency injection
	public IRepository Repo { get; } = ServiceHelper.GetService<IRepository>()!;
	public IDataService DataService { get; } = ServiceHelper.GetService<IDataService>()!;

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	string _title;

	public virtual async Task InitializeAsync<T>()
	{
		// TODO Initialize?
		await Task.CompletedTask;
	}
}
