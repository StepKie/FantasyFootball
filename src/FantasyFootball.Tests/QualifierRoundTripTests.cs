namespace FantasyFootball.Tests;

/// <summary>
/// Regression guard for the SQLite multi-OneToOne-same-type cascade bug:
/// KoGame has two GroupQualifier nav properties (Home/Away) and sqlite-net-extensions
/// stomps the first FK with the second child's Id during cascade insert. Repository
/// works around it via PreInsert + post-cascade SQL fixup. This test asserts the
/// fixup is in place — without it, all KoGames end up with HomeGroupQualifier ==
/// AwayGroupQualifier after the round-trip InitCompetition does.
/// </summary>
public class QualifierRoundTripTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Warning)
{
	[InlineData(CompetitionType.WM, 2022)]
	[InlineData(CompetitionType.WM, 2026)]
	[InlineData(CompetitionType.EM, 2024)]
	[Theory]
	public void KoGameQualifierPairs_AreDistinctAfterRoundTrip(CompetitionType type, int year)
	{
		var competition = InitCompetition(type, year);

		var koGames = competition.Stages
			.SelectMany(s => s.Rounds)
			.SelectMany(r => r.KoGames)
			.ToList();

		koGames.Should().NotBeEmpty($"{type} {year} must have KO games");

		foreach (var ko in koGames)
		{
			// Either both Group qualifiers are set (R16 of a knockout from groups)
			// or both Game qualifiers are set (QF/SF/Final from prior KO winners),
			// or a mix (e.g., 3rd-place placements). In every case the two slots
			// (Home + Away) must refer to distinct rows.
			if (ko.HomeGroupQualifier is not null && ko.AwayGroupQualifier is not null)
			{
				ko.HomeGroupQualifier.Id.Should().NotBe(ko.AwayGroupQualifier.Id,
					$"KoGame Id={ko.Id} ({ko.Round?.Name}): Home and Away GroupQualifier must be distinct rows after SQLite round-trip");
				ReferenceEquals(ko.HomeGroupQualifier, ko.AwayGroupQualifier).Should().BeFalse(
					$"KoGame Id={ko.Id}: Home and Away GroupQualifier must be distinct instances");
			}
			if (ko.HomeGameQualifier is not null && ko.AwayGameQualifier is not null)
			{
				ko.HomeGameQualifier.Id.Should().NotBe(ko.AwayGameQualifier.Id,
					$"KoGame Id={ko.Id} ({ko.Round?.Name}): Home and Away GameQualifier must be distinct rows after SQLite round-trip");
				ReferenceEquals(ko.HomeGameQualifier, ko.AwayGameQualifier).Should().BeFalse(
					$"KoGame Id={ko.Id}: Home and Away GameQualifier must be distinct instances");
			}
		}

		// Cover the UPDATE path too — InitCompetition exercises Repo.Save with Id == 0
		// (insert branch). Re-save and reload to exercise the Id != 0 branch in
		// Repository.Save, which has its own snapshot/restore + UpdateAllGames flow.
		Repo.Save(competition);
		var reloaded = Repo.Get<Competition>(competition.Id);
		reloaded.Should().NotBeNull("update-path Save must keep the competition retrievable");

		var reloadedKoGames = reloaded!.Stages.SelectMany(s => s.Rounds).SelectMany(r => r.KoGames).ToList();
		reloadedKoGames.Should().NotBeEmpty($"{type} {year} (update path) must still have KO games after Save+reload");
		foreach (var ko in reloadedKoGames)
		{
			if (ko.HomeGroupQualifier is not null && ko.AwayGroupQualifier is not null)
			{
				ko.HomeGroupQualifier.Id.Should().NotBe(ko.AwayGroupQualifier.Id,
					$"KoGame Id={ko.Id} (update path): Home and Away GroupQualifier must stay distinct after Save+reload");
			}
			if (ko.HomeGameQualifier is not null && ko.AwayGameQualifier is not null)
			{
				ko.HomeGameQualifier.Id.Should().NotBe(ko.AwayGameQualifier.Id,
					$"KoGame Id={ko.Id} (update path): Home and Away GameQualifier must stay distinct after Save+reload");
			}
		}
	}
}
