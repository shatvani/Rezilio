using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rezilio.Modules.Licensing.Domain.Exceptions;

namespace Rezilio.Api.Middleware;

/// <summary>
/// Szabványos ASP.NET Core IExceptionHandler: a ModuleNotLicensedException-t 403-as
/// ProblemDetails válasszá alakítja, bárhol is dobódik (Wolverine handler chain, HTTP vagy
/// belső üzenetbusz-hívás közben).
/// </summary>
public sealed class ModuleNotLicensedExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ModuleNotLicensedException moduleNotLicensedException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Modul nincs aktiválva.",
                Detail = moduleNotLicensedException.Message,
            },
            cancellationToken);

        return true;
    }
}
