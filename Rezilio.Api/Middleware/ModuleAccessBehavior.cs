using System.Reflection;
using Rezilio.Modules.Licensing;
using Rezilio.Modules.Licensing.Application.Services;
using Rezilio.Modules.Licensing.Domain.Exceptions;
using Wolverine;

namespace Rezilio.Api.Middleware;

/// <summary>
/// Wolverine pipeline middleware – modul licensz-ellenőrzés.
/// Csak azokra a command/query típusokra fut le érdemben, amikre fel van téve a
/// //see cref="RequiresModuleAttribute"/// — ilyen jelenleg még nincs egyetlen modulban sem
/// (az Organization modul szándékosan nincs licenszelve), de a jövőbeli prémium modulok
/// (Risk/Assessment/Treatment stb.) parancsai erre lesznek felkészítve.
/// </summary>
public class ModuleAccessBehavior
{
    public async Task<HandlerContinuation> BeforeAsync(
        Envelope envelope,
        IModuleAccessChecker moduleAccessChecker,
        ILogger<ModuleAccessBehavior> logger,
        CancellationToken cancellationToken)
    {
        object? message = envelope.Message;
        if (message is null)
        {
            return HandlerContinuation.Continue;
        }

        Type messageType = message.GetType();
        RequiresModuleAttribute? attribute = messageType.GetCustomAttribute<RequiresModuleAttribute>();

        if (attribute is null)
        {
            // Ez a command/query nincs licenszhez kötve (pl. Organization modul) — mindig engedélyezett.
            return HandlerContinuation.Continue;
        }

        PropertyInfo? tenantIdProperty = messageType.GetProperty("TenantId");
        if (tenantIdProperty?.GetValue(message) is not Guid tenantId)
        {
            logger.LogWarning(
                "{MessageType} [RequiresModule] attribútummal van jelölve, de nincs olvasható TenantId property — a modul-ellenőrzés kihagyva.",
                messageType.Name);
            return HandlerContinuation.Continue;
        }

        bool isActive = await moduleAccessChecker.IsModuleActiveAsync(attribute.Module, tenantId, cancellationToken);
        if (!isActive)
        {
            throw new ModuleNotLicensedException(attribute.Module);
        }

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
