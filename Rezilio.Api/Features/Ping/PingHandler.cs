using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Rezilio.Api.Features.Ping;

public static class PingHandler
{
    /// <summary>
    /// Story 0.5 smoke test: csak érvényes Keycloak JWT-vel érhető el.
    /// 401 → nincs token vagy lejárt
    /// 200 → auth működik
    /// </summary>
    [WolverineGet("/ping")]
    [Authorize]
    public static string Handle()
        => "Pong";
}
