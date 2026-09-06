namespace Rezilio.Modules.Organization.Application.Commands.LinkKeyPersonToUser;

/// <summary>
/// Admin-only kézi identitás-kötés: akkor kell, ha az automatikus JIT-linking
/// (ld. CurrentUserContext.GetKeyPersonIdAsync) nem tud egyértelműen kötni —
/// pl. a KeyPerson.Email eltér a Keycloak-email címtől, vagy a KeyPerson-nek
/// egyáltalán nincs email címe megadva.
/// </summary>
public sealed record LinkKeyPersonToUserCommand(Guid KeyPersonId, string UserId);
