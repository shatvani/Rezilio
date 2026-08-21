using Wolverine;

namespace Rezilio.Api.Middleware;

/// <summary>
/// Wolverine pipeline middleware – modul licensz-ellenőrzés.
/// Phase 0.2: skeleton, mindig átenged.
/// Phase 0.5: tényleges <c>ModuleType</c>-alapú ellenőrzés kerül ide.
/// </summary>
public class ModuleAccessBehavior
{
    public async Task<HandlerContinuation> BeforeAsync(
        Envelope envelope,
        ILogger<ModuleAccessBehavior> logger,
        CancellationToken cancellationToken)
    {
        // TODO Phase 0.5: resolválni a szükséges ModuleType-ot a command attribútumból,
        // majd ITenantLicenseService.IsModuleActiveAsync() hívás.
        // Ha false → throw new ModuleNotLicensedException(moduleType);

        logger.LogDebug("ModuleAccessBehavior: {MessageType} – engedélyezve (Phase 0.2 placeholder)",
            envelope.MessageType);

        return HandlerContinuation.Continue;
    }
}

/*
 * A middleware pipeline a következőképpen épül fel:
 * - A Wolverine a MessageType-hoz tartozó HandlerChain-t hozza létre.
 * - A HandlerChain-hez hozzáadódik a ModuleAccessBehavior, majd a tényleges handler.
 * - A ModuleAccessBehavior.BeforeAsync() fut le először, majd ha az engedélyezett,
 *   akkor a tényleges handler.HandleAsync() fut le.
 */
/*
 * HTTP kérés
    → Wolverine pipeline
        → [ModuleAccessBehavior.BeforeAsync]
              Megvizsgálja: melyik modulhoz tartozik ez a command?
              Van-e ennek a tenantnak aktív licensze erre a modulra?
              NEM → 403 + ModuleNotLicensedException (handler NEM fut le)
              IGEN → folytatás
        → [tényleges Handler]
*/
