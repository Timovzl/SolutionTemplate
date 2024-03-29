namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain;

/// <summary>
/// Abstract base type for a domain event, i.e. a relevant thing that has happened within the domain model.
/// </summary>
public abstract class DomainEvent<TId> : ValueObject, IDomainEvent
	where TId : IEquatable<TId>?, IComparable<TId>?
{
	public override string ToString() => $"{{{this.GetType().Name} Id={this.Id}}}";
	public override int GetHashCode() => throw new NotImplementedException("Structural equality for events is not implemented by default.");
	public override bool Equals(object? obj) => throw new NotImplementedException("Structural equality for events is not implemented by default.");

	public TId Id { get; }

	protected DomainEvent(TId id)
	{
		this.Id = id;

		// If constructing a new object (not reconstituting an existing one), track it
		if (!EqualityComparer<TId>.Default.Equals(id, default))
			DomainObjectTracker.DidCreateOrphanedDomainObject(this);
	}
}

public interface IDomainEvent : IDomainObject
{
}
