namespace FantasyFootball.Tests;

/// <summary>
/// Unit tests for the JSON → Competition loader. Drives the parser
/// off small handwritten JSON literals; the real competition files (WC2026,
/// WC2022, EM2024) get exercised in a separate integration test once
/// they're authored.
/// </summary>
public class CompetitionDefinitionLoaderTests
{
	const string MiniDefinition = """
		{
		  "id": "mini-test",
		  "title": "Mini Test Cup",
		  "type": "WM",
		  "year": 2026,
		  "formatId": "mini-test",
		  "stages": [
		    { "id": "group", "name": "Group Stage", "order": 0 },
		    { "id": "ko",    "name": "Knockout",    "order": 1 }
		  ],
		  "rounds": [
		    { "id": "g-r1",  "name": "Matchday 1", "stageId": "group", "order": 0 },
		    { "id": "final", "name": "Final",      "stageId": "ko",    "order": 1 }
		  ],
		  "groups": {
		    "A": ["MEX", "RSA"],
		    "B": ["BRA", "ARG"]
		  },
		  "games": [
		    {
		      "kind": "group", "id": 1,
		      "playedOn": "2026-06-01T16:00:00",
		      "roundId": "g-r1",
		      "groupLetter": "A",
		      "venueId": "azteca",
		      "homeTeamId": "MEX",
		      "awayTeamId": "RSA"
		    },
		    {
		      "kind": "group", "id": 2,
		      "playedOn": "2026-06-01T20:00:00",
		      "roundId": "g-r1",
		      "groupLetter": "B",
		      "venueId": "maracana",
		      "homeTeamId": "BRA",
		      "awayTeamId": "ARG"
		    },
		    {
		      "kind": "ko", "id": 3,
		      "playedOn": "2026-06-28T20:00:00",
		      "roundId": "final",
		      "venueId": "azteca",
		      "homeQual": "A1",
		      "awayQual": "B1"
		    }
		  ]
		}
		""";

	[Fact]
	public void Load_TopLevelFields_PopulatedFromJson()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		c.DefinitionId.Should().Be("mini-test");
		c.Title.Should().Be("Mini Test Cup");
		c.Type.Should().Be(CompetitionType.WM);
		c.Year.Should().Be(2026);
		c.FormatId.Should().Be("mini-test");
	}

	[Fact]
	public void Load_StagesAndRounds_ParsedInOrder()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		c.Stages.Should().HaveCount(2);
		c.Stages[0].Should().Be(new Stage("group", "Group Stage", 0));
		c.Stages[1].Should().Be(new Stage("ko", "Knockout", 1));
		c.Rounds.Should().HaveCount(2);
		c.Rounds[0].Should().Be(new Round("g-r1", "Matchday 1", "group", 0));
		c.Rounds[1].Should().Be(new Round("final", "Final", "ko", 1));
	}

	[Fact]
	public void Load_GroupsDictionary_ConvertedToOrderedArray()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		c.GroupAssignments.Should().HaveCount(2);
		c.GroupAssignments[0].Should().BeEquivalentTo(["MEX", "RSA"]);
		c.GroupAssignments[1].Should().BeEquivalentTo(["BRA", "ARG"]);
	}

	[Fact]
	public void Load_PolymorphicGames_DiscriminatedByKind()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		c.Games.Should().HaveCount(3);
		c.Games[0].Should().BeOfType<GroupGame>();
		c.Games[1].Should().BeOfType<GroupGame>();
		c.Games[2].Should().BeOfType<KoGame>();
	}

	[Fact]
	public void Load_GroupGame_FieldsAndScheduledState()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		var g = (GroupGame)c.Games[0];
		g.Id.Should().Be(1);
		g.PlayedOn.Should().Be(new DateTime(2026, 6, 1, 16, 0, 0));
		g.RoundId.Should().Be("g-r1");
		g.GroupLetter.Should().Be("A");
		g.VenueId.Should().Be("azteca");
		g.HomeTeamId.Should().Be("MEX");
		g.AwayTeamId.Should().Be("RSA");
		g.Result.Should().BeNull("definition is the schedule — no results yet");
	}

	[Fact]
	public void Load_KoGame_CarriesQualifierStrings()
	{
		var c = CompetitionDefinitionLoader.Load(MiniDefinition);
		var ko = (KoGame)c.Games[2];
		ko.HomeQual.Should().Be("A1");
		ko.AwayQual.Should().Be("B1");
		ko.HomeTeamId.Should().BeNull("KO qualifiers haven't resolved yet");
		ko.AwayTeamId.Should().BeNull();
		ko.Result.Should().BeNull();
	}

	[Fact]
	public void Load_RejectsUnknownStageReferenceFromRound()
	{
		var bad = MiniDefinition.Replace("\"stageId\": \"group\"", "\"stageId\": \"phantom\"");
		Action act = () => CompetitionDefinitionLoader.Load(bad);
		act.Should().Throw<FormatException>().WithMessage("*phantom*");
	}

	[Fact]
	public void Load_RejectsUnknownRoundReferenceFromGame()
	{
		var bad = MiniDefinition.Replace("\"roundId\": \"final\"", "\"roundId\": \"phantom-round\"");
		Action act = () => CompetitionDefinitionLoader.Load(bad);
		act.Should().Throw<FormatException>().WithMessage("*phantom-round*");
	}

	[Fact]
	public void Load_RejectsGapInGroupAlphabet()
	{
		// Drop group B; loader requires sequential letters.
		var bad = MiniDefinition.Replace("\"B\": [\"BRA\", \"ARG\"]", "\"C\": [\"BRA\", \"ARG\"]");
		Action act = () => CompetitionDefinitionLoader.Load(bad);
		act.Should().Throw<FormatException>().WithMessage("*'B' missing*");
	}

	[Fact]
	public void Load_DefinitionWithCommentsAndTrailingCommas_StillParses()
	{
		var withComments = """
			{
			  // top-level comment
			  "id": "mini-test",
			  "title": "Mini Test Cup",
			  "type": "WM",
			  "year": 2026,
			  "formatId": "mini-test",
			  "stages": [
			    { "id": "group", "name": "Group", "order": 0 },
			  ],
			  "rounds": [
			    { "id": "r1", "name": "Round 1", "stageId": "group", "order": 0 },
			  ],
			  "groups": { "A": ["X", "Y"] },
			  "games": [
			    /* one tiny game */
			    { "kind": "group", "id": 1, "playedOn": "2026-06-01T16:00:00", "roundId": "r1",
			      "groupLetter": "A", "homeTeamId": "X", "awayTeamId": "Y" },
			  ],
			}
			""";
		var c = CompetitionDefinitionLoader.Load(withComments);
		c.Games.Should().ContainSingle();
	}
}
