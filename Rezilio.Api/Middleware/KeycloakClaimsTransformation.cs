using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Rezilio.SharedKernel.Auth;

namespace Rezilio.Api.Middleware;

/// <summary>
/// Keycloak JWT claim-eket normalizálja AppClaims konstansokra.
///
/// A Keycloak realm a következő claim-eket állítja elő a rezilio-api kliensen:
///   app:user_id   ← Keycloak user UUID (user model id property)
///   app:tenant_id ← felhasználó tenant_id attribútuma
///   app:email     ← felhasználó e-mail címe
///   app:roles     ← realm szerepkörök (multivalued)
///
/// Ha az app:user_id nincs jelen (pl. a user.attribute mapper nem működik),
/// fallback-ként a standard "sub" claim-et használja (ugyanaz az érték).
///
/// Handler-ekben kizárólag AppClaims konstansokat szabad használni,
/// nem a nyers Keycloak claim neveket.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Ne módosítsd az eredetit — klónozz
        ClaimsIdentity identity = new(principal.Identity);

        // app:user_id — ha a realm mapper nem adta, fallback: sub
        if (!principal.HasClaim(c => c.Type == AppClaims.UserId))
        {
            string? sub = principal.FindFirstValue("sub");
            if (sub is not null)
            {
                identity.AddClaim(new Claim(AppClaims.UserId, sub));
            }
        }

        // app:roles → ClaimTypes.Role, hogy az [Authorize(Roles = "...")] működjön
        foreach (Claim roleClaim in principal.FindAll(AppClaims.Roles))
        {
            if (!principal.HasClaim(ClaimTypes.Role, roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
            }
        }

        ClaimsPrincipal result = new(principal);
        result.AddIdentity(identity);
        return Task.FromResult(result);
    }
}
