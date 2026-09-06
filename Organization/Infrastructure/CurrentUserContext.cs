using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rezilio.SharedKernel.Auth;
using Rezilio.SharedKernel.Multitenancy;

namespace Rezilio.Modules.Organization.Infrastructure;

/// <summary>
/// ICurrentUserContext implementáció. A KeyPerson-lookup/JIT-linking logikát azért
/// itt, az Organization modulban valósítjuk meg (nem a SharedKernel-ben), mert
/// egyedül ez a modul birtokolja a KeyPerson aggregátumot és az OrganizationDbContext-et —
/// a SharedKernel-ben csak az interfész (ICurrentUserContext) él, ahogy az
/// ITenantContext/FixedTenantContext párosnál is.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        OrganizationDbContext db,
        ITenantContext tenantContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _tenantContext = tenantContext;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(AppClaims.UserId);

    public string? Email => Principal?.FindFirstValue(AppClaims.Email);

    public async Task<Guid?> GetKeyPersonIdAsync(CancellationToken ct = default)
    {
        var userId = UserId;
        if (userId is null)
        {
            return null;
        }

        var tenantId = _tenantContext.TenantId;

        // 1. lépés: már kötött KeyPerson keresése UserId alapján — ez a gyakori eset
        // minden bejelentkezés után, miután a JIT-linking egyszer lefutott.
        var linkedId = await _db.KeyPersons
            .Where(k => k.TenantId == tenantId && k.UserId == userId)
            .Select(k => (Guid?)k.Id)
            .FirstOrDefaultAsync(ct);

        if (linkedId is not null)
        {
            return linkedId;
        }

        // 2. lépés: JIT-linking email-egyezés alapján. Csak akkor kötünk automatikusan,
        // ha pontosan egy aktív KeyPerson egyezik az email címmel — kétértelmű (0 vagy
        // több egyezés) esetben null-t adunk vissza, és az Admin-nak kézzel kell kötnie
        // (LinkKeyPersonToUser Command).
        var email = Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var candidates = await _db.KeyPersons
            .Where(k => k.TenantId == tenantId
                     && k.IsActive
                     && k.Email != null
                     && k.Email.ToLower() == normalizedEmail)
            .ToListAsync(ct);

        if (candidates.Count != 1)
        {
            return null;
        }

        var keyPerson = candidates[0];
        keyPerson.LinkToUser(userId);
        await _db.SaveChangesAsync(ct);

        return keyPerson.Id;
    }
}
