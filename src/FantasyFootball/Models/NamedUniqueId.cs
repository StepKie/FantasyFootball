namespace FantasyFootball.Models;

public abstract class NamedUniqueId : IComparable
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public virtual string Name { get; set; }

	/// <summary> Public parameterless constructor necessary for SQLite </summary>
	public NamedUniqueId() { }

	public override string ToString() => Name;

	public override bool Equals(object? obj) => GetType() == obj?.GetType() && Id != 0 && Id == (obj as NamedUniqueId)?.Id;

	public override int GetHashCode() => HashCode.Combine(Id, Name, GetType().Name);

	public int CompareTo(object? obj) => Id.CompareTo((obj as NamedUniqueId)?.Id);
}
