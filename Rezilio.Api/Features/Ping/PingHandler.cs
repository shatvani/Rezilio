using Wolverine.Http;

namespace Rezilio.Api.Features.Ping;

public static class PingHandler
{
    [WolverineGet("/ping")]
    public static string Handle(string message)
        => $"Pong: {message}";
}
