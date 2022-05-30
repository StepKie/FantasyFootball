﻿namespace FantasyFootball.Models;

[Table(nameof(Stage))]
public class Stage : NamedUniqueId
{
	/// <summary> Holds the participants. May only be a single Group (in case of a National League for example ...) </summary>
	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public List<Group> Groups { get; set; } = new();

	[OneToMany(CascadeOperations = CascadeOperation.All)]
	public virtual List<Round> Rounds { get; set; }

	[ForeignKey(typeof(Competition))]
	public int CompetitionId { get; set; }

	[ManyToOne]
	public Competition Competition { get; }

	[Ignore] public Round? CurrentRound => Rounds.FirstOrDefault(r => !r.IsFinished);
	[Ignore] public IList<Game> Games => Rounds.SelectMany(round => round.Games).OrderBy(g => g.PlayedOn).ToList();
	[Ignore] public bool IsFinished => CurrentRound == null;
}