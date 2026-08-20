namespace Rezilio.SharedKernel.DDD;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
