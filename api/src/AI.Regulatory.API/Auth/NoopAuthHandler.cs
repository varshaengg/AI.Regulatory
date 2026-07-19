using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Auth;

/// <summary>
/// No-op authentication handler used when EntraId is not configured (dev / smoke deploy).
/// Returns NoResult so [AllowAnonymous] endpoints work and [Authorize] endpoints get 401.
/// </summary>
internal sealed class NoopAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoopAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}
