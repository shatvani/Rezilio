using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

/// <summary>
/// A KeyPerson össze lett kötve egy Keycloak-felhasználóval (identitás-kötés) —
/// akár automatikus JIT-linking (email-egyezés első bejelentkezéskor), akár Admin
/// által kézzel elvégzett LinkKeyPersonToUser Command útján.
/// </summary>
public sealed record KeyPersonLinkedToUser(Guid KeyPersonId, Guid TenantId, string UserId) : DomainEvent;
