namespace Rezilio.SharedKernel.DDD.Messaging;

public interface IEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}

public abstract record DomainEvent : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().Name;  // automatikus, nem kell kézzel írni
}
