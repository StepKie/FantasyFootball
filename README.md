# Fantasy Football

Simulate football competitions for mobile devices

In a first iteration, this should work for hardcoded World and European Championships

## TODOS

- TeamViewModel/TeamPage:
	- allow editing name, elo
	- allow creating new team
	
- Game
	- allow setting result on unfinished games
	
- CompetitionDetailViewModel
	- Singleton creates problems when sharing between StatisticsPage and GroupStandings
	- Also CompetitionsPage really wants CompetitionViewModels, currently certain properties needed for display only are in the model (Competition)
	- use in CompetitionsPage to show ActivityIndicator when running, move CurrentStatus from Model to ViewModel etc

- CompetitionSetupPage
	- batch simulation: Navigation to previous page not happening

- StandingsPage
	- Not updating/Not showing?

- IDataService
	- more robust initialization routine
	
- Fix various TODO tags in code
- Bindings not updating for certain CollectionViews during OnNavigatedTo()

- Missing Localizations
- Performance/Jankiness

- Longterm
	- Add Stadiums, Cities, Winning Probabilities(?) to Game
