# Fantasy Football

Simulate football competitions for mobile devices

In a first iteration, this should work for hardcoded World and European Championships

## TODOS

- CompetitionsPage
	- Add Replay Button (10x)
	- Remove unused competitions
	- Rename CompetitionName to integrate Id when multiple Competitions of the same type
	
- CompetitionDetailViewModel
	- Singleton creates problems when sharing between StatisticsPage and GroupStandings
	- Also CompetitionsPage really wants CompetitionViewModels, currently certain properties needed for display only are in the model (Competition)

- Add: CompetitionSetupPage
	- With Drawing, Manual Selection go to a new page
	- looks like Groups (StandingsPage), where you can select Teams via Picker or click to add a random team - one-by-one or all at once

- RoundAdvancer
	- For EM (FIX)
	- For WM (new)
	- integrate in CompetitionFactory (?)

- Fix various TODO tags in code
- Bindings not updating for certain CollectionViews during OnNavigatedTo()

- Missing Localizations
- Performance/Jankiness

- Longterm
	- Add Stadiums, Cities, Winning Probabilities(?) to Game
