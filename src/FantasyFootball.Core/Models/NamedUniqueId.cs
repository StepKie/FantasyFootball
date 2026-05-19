namespace FantasyFootball.Models;

public abstract class NamedUniqueId
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public virtual string Name { get; init; }

	/// <summary> Public parameterless constructor necessary for SQLite </summary>
	public NamedUniqueId() { }

	public override string ToString() => Name;

	/// <summary>
	/// Reference-equal first (so the same in-memory instance is equal to itself even before
	/// an Id has been assigned by persistence). Otherwise type + non-zero Id match.
	/// Without the ReferenceEquals shortcut, freshly-constructed entities (Stage / Round /
	/// Group / Game — none of which the LocalStorage path assigns Ids to) compared false
	/// even against themselves, which broke MudSelect's Item-vs-Value matching and put
	/// Blazor's render loop into an infinite ValueChanged→state-change→re-render cycle.
	/// </summary>
	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(this, obj)) { return true; }
		if (obj is null || GetType() != obj.GetType()) { return false; }
		return Id != 0 && Id == ((NamedUniqueId)obj).Id;
	}

	public override int GetHashCode() => HashCode.Combine(Id, Name, GetType().Name);
}
