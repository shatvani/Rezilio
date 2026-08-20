namespace Rezilio.SharedKernel.DDD.VOs;

public sealed class TenantId : ValueObject
{
    /// <summary>Phase 1: egyetlen fix tenant. Phase 2-ben eltávolítandó.</summary>
    public static readonly TenantId Default = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public Guid Value { get; }

    public TenantId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("TenantId nem lehet üres GUID.", nameof(value));
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
