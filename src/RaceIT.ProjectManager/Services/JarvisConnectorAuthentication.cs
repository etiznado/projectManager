using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceIT.ProjectManager.Services;

/// <summary>Immutable operator configuration shared by authentication and front authorization.</summary>
public sealed class JarvisConnectorAccess
{
    public const string Scheme = "JarvisConnector";
    public bool Enabled { get; }
    public string ConnectorId { get; }
    public int PersonId { get; }
    public bool AllowAllOwnerFronts { get; }
    private readonly byte[] tokenHash;
    private readonly int[] allowedFrontIds;

    private JarvisConnectorAccess(bool enabled, string connectorId, int personId, byte[] hash, int[] ids, bool allowAllOwnerFronts = false)
    {
        Enabled = enabled;
        ConnectorId = connectorId;
        PersonId = personId;
        AllowAllOwnerFronts = allowAllOwnerFronts;
        tokenHash = hash;
        allowedFrontIds = ids;
    }

    public static JarvisConnectorAccess Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication:Jarvis");
        if (!section.GetValue<bool>("Enabled")) return new(false, "", 0, [], []);
        var id = section["ConnectorId"] ?? "";
        var hash = section["TokenSha256"] ?? "";
        var owner = section.GetValue<int>("PersonId");
        var grants = section.GetSection("AllowedFrontIds").Get<int[]>() ?? [];
        if (id.Length is < 1 or > 200 || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.')) ||
            owner <= 0 || hash.Length != 64 || hash.Any(c => !char.IsAsciiHexDigit(c)) ||
            grants.Any(x => x <= 0) || grants.Distinct().Count() != grants.Length)
            throw new InvalidOperationException("La configuración privada de Jarvis no es válida.");
        return new(true, id, owner, Convert.FromHexString(hash), grants.ToArray(), section.GetValue<bool>("AllowAllOwnerFronts"));
    }

    internal int[] FrontIds => allowedFrontIds.ToArray();

    internal bool AcceptsToken(string token) => Enabled && token.Length is >= 43 and <= 512 &&
        token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '+' or '/' or '=') &&
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(token)), tokenHash);

    internal void RequireIdentity(ClaimsPrincipal actor)
    {
        if (!Enabled || !actor.Identities.Any(identity => identity.IsAuthenticated && identity.AuthenticationType == Scheme &&
            identity.FindFirst("auth_source")?.Value == "jarvis" &&
            identity.FindFirst(ClaimTypes.NameIdentifier)?.Value == $"connector:{ConnectorId}"))
            throw new UnauthorizedAccessException("Acceso de conector no disponible.");
    }
}

public static class JarvisConnectorRegistration
{
    public static IServiceCollection AddJarvisConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(JarvisConnectorAccess.Read(configuration));
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, JarvisConnectorHandler>(JarvisConnectorAccess.Scheme, _ => { });
        services.AddAuthorization(options => options.AddPolicy(JarvisConnectorAccess.Scheme, policy =>
            policy.AddAuthenticationSchemes(JarvisConnectorAccess.Scheme).RequireAuthenticatedUser()));
        return services;
    }
}

public sealed class JarvisConnectorHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, JarvisConnectorAccess access) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers.Authorization;
        if (!access.Enabled || !Request.IsHttps || headers.Count != 1)
            return Task.FromResult(AuthenticateResult.NoResult());
        var header = headers[0] ?? "";
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || !access.AcceptsToken(header[7..]))
            return Task.FromResult(AuthenticateResult.Fail("Credencial de conector no válida."));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, $"connector:{access.ConnectorId}"),
            new Claim(ClaimTypes.Name, access.ConnectorId), new Claim("auth_source", "jarvis")], JarvisConnectorAccess.Scheme));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Response.WriteAsJsonAsync(new { error = "unauthenticated" });
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Response.WriteAsJsonAsync(new { error = "forbidden" });
    }
}
