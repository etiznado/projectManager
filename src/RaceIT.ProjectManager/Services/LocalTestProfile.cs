using System.Data.Common;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaceIT.ProjectManager.Data;

namespace RaceIT.ProjectManager.Services;

/// <summary>Explicit, independently approved synthetic profile. Normal startup never loads its private files.</summary>
public sealed class LocalTestProfile
{
    public const string CookieName = "RaceIT.LocalTest.Session";
    private const string Invalid = "El perfil local de prueba no está aprobado o no es válido.";
    private readonly LocalTestSettings settings;
    private string? planningConfigurationPath;
    public Uri UiAddress { get; }
    public Uri ApiAddress { get; }
    public string ConnectionString => settings.ConnectionString;
    public string PublicCertificatePath => settings.PublicCertificatePath;

    private LocalTestProfile(LocalTestSettings settings)
    {
        this.settings = settings with { AllowedFrontIds = settings.AllowedFrontIds.ToArray() };
        UiAddress = new Uri(settings.UiUrl);
        ApiAddress = new Uri(settings.ApiUrl);
    }

    public static LocalTestProfile? Load(string environment, IConfiguration configuration)
    {
        var section = configuration.GetSection("LocalTestProfile");
        var directory = section["Directory"];
        if (!section.GetValue<bool>("Enabled") && string.IsNullOrEmpty(directory)) return null;
        try
        {
            if (!section.GetValue<bool>("Enabled") || string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
                throw new InvalidOperationException(Invalid);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
            var runtime = JsonSerializer.Deserialize<LocalTestSettings>(File.ReadAllText(Path.Combine(directory, "runtime.json")), options);
            var approval = JsonSerializer.Deserialize<LocalTestSettings>(File.ReadAllText(Path.Combine(directory, "approval.json")), options);
            var profile = Validate(environment, runtime!, approval!, configuration);
            profile.planningConfigurationPath = Path.Combine(directory, "planning.json");
            return profile;
        }
        catch (Exception) { throw new InvalidOperationException(Invalid); }
    }

    public static LocalTestProfile Validate(string environment, LocalTestSettings runtime, LocalTestSettings approval, IConfiguration configuration)
    {
        if (environment != "PrivateQa" || runtime is null || approval is null || !runtime.ExplicitlyEnabled || !runtime.SyntheticDataOnly ||
            !runtime.SelectorEnabled || !runtime.JarvisEnabled || !approval.ExplicitlyEnabled || !approval.SyntheticDataOnly ||
            !approval.SelectorEnabled || !approval.JarvisEnabled || string.IsNullOrWhiteSpace(runtime.ProfileId) ||
            runtime.ProfileId != approval.ProfileId || runtime.ConnectorId != approval.ConnectorId ||
            runtime.PersonId <= 0 || runtime.PersonId != approval.PersonId || runtime.AllowedFrontIds is null || approval.AllowedFrontIds is null ||
            !runtime.AllowedFrontIds.SequenceEqual(approval.AllowedFrontIds) || runtime.AllowedFrontIds.Any(x => x <= 0) ||
            runtime.AllowedFrontIds.Distinct().Count() != runtime.AllowedFrontIds.Length ||
            runtime.AllowAllOwnerFronts != approval.AllowAllOwnerFronts ||
            runtime.TokenSha256.Length != 64 || runtime.TokenSha256.Any(x => !char.IsAsciiHexDigit(x)) ||
            !string.Equals(runtime.TokenSha256, approval.TokenSha256, StringComparison.OrdinalIgnoreCase) ||
            runtime.UiUrl != approval.UiUrl || runtime.ApiUrl != approval.ApiUrl ||
            runtime.PfxPath != approval.PfxPath || runtime.PfxPasswordDpapiPath != approval.PfxPasswordDpapiPath ||
            runtime.PublicCertificatePath != approval.PublicCertificatePath ||
            configuration.GetSection("Kestrel").GetChildren().Any() ||
            new[] { "urls", "http_ports", "https_ports" }.Any(key => !string.IsNullOrEmpty(configuration[key])))
            throw new InvalidOperationException(Invalid);
        try
        {
            var actual = ParseConnection(runtime.ConnectionString);
            var pinned = ParseConnection(approval.ConnectionString);
            if (!string.Equals(actual.ConnectionString, pinned.ConnectionString, StringComparison.Ordinal))
                throw new InvalidOperationException(Invalid);
            foreach (var path in new[] { runtime.PfxPath, runtime.PfxPasswordDpapiPath, runtime.PublicCertificatePath })
                if (!Path.IsPathFullyQualified(path)) throw new InvalidOperationException(Invalid);
            ValidateAddress(runtime.UiUrl, runtime.UiUrl == runtime.ApiUrl ? "https" : "http");
            ValidateAddress(runtime.ApiUrl, "https");
            if (runtime.UiUrl != runtime.ApiUrl && new Uri(runtime.UiUrl).Port == new Uri(runtime.ApiUrl).Port)
                throw new InvalidOperationException(Invalid);
            var candidate = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jarvis:Enabled"] = "true", ["Authentication:Jarvis:ConnectorId"] = runtime.ConnectorId,
                ["Authentication:Jarvis:PersonId"] = runtime.PersonId.ToString(), ["Authentication:Jarvis:TokenSha256"] = runtime.TokenSha256,
                ["Authentication:Jarvis:AllowAllOwnerFronts"] = runtime.AllowAllOwnerFronts.ToString()
            }.Concat(runtime.AllowedFrontIds.Select((id, index) => new KeyValuePair<string, string?>("Authentication:Jarvis:AllowedFrontIds:" + index, id.ToString())))).Build();
            _ = JarvisConnectorAccess.Read(candidate);
            return new LocalTestProfile(runtime);
        }
        catch (Exception) { throw new InvalidOperationException(Invalid); }
    }

    private static SqlConnectionStringBuilder ParseConnection(string value)
    {
        var parsed = new SqlConnectionStringBuilder(value);
        if (string.IsNullOrWhiteSpace(parsed.DataSource) || string.IsNullOrWhiteSpace(parsed.InitialCatalog) || !parsed.IntegratedSecurity ||
            !string.IsNullOrEmpty(parsed.AttachDBFilename) || !string.IsNullOrEmpty(parsed.FailoverPartner) ||
            !string.IsNullOrEmpty(parsed.UserID) || !string.IsNullOrEmpty(parsed.Password)) throw new InvalidOperationException(Invalid);
        return parsed;
    }

    private static void ValidateAddress(string value, string scheme)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != scheme || !IPAddress.TryParse(uri.Host, out var ip) ||
            !IPAddress.IsLoopback(ip) || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            uri.UserInfo.Length != 0 || uri.Port <= 0) throw new InvalidOperationException(Invalid);
    }

    public bool AllowsSelector(IPAddress? remote) => remote is not null && IPAddress.IsLoopback(remote);

    public void Configure(WebApplicationBuilder builder)
    {
        try
        {
            if (!File.Exists(settings.PfxPath) || !File.Exists(settings.PfxPasswordDpapiPath)) throw new InvalidOperationException(Invalid);
            var password = Environment.GetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD");
            Environment.SetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD", null);
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException(Invalid);
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(settings.PfxPath, password, X509KeyStorageFlags.Exportable);
            if (!certificate.HasPrivateKey || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow)
                throw new InvalidOperationException(Invalid);
            builder.Services.AddSingleton<X509Certificate2>(_ => certificate);
            builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(10));
            // Replace general settings only after independent approval; no configured endpoints or URL fallbacks remain.
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProjectManager"] = settings.ConnectionString,
                ["Authentication:Google:Enabled"] = "false", ["Authentication:Selector:Enabled"] = "true",
                ["Authentication:Selector:SyntheticDataOnly"] = "true", ["Authentication:Jarvis:Enabled"] = "true",
                ["Authentication:Jarvis:ConnectorId"] = settings.ConnectorId, ["Authentication:Jarvis:PersonId"] = settings.PersonId.ToString(),
                ["Authentication:Jarvis:TokenSha256"] = settings.TokenSha256,
                ["Authentication:Jarvis:AllowAllOwnerFronts"] = settings.AllowAllOwnerFronts.ToString()
            }.Concat(settings.AllowedFrontIds.Select((id, index) => new KeyValuePair<string, string?>("Authentication:Jarvis:AllowedFrontIds:" + index, id.ToString()))));
            if (planningConfigurationPath is not null && File.Exists(planningConfigurationPath))
            {
                // This operator file can configure only Planning, never the pinned target or credential.
                var planning = new ConfigurationBuilder().AddJsonFile(planningConfigurationPath, optional: false, reloadOnChange: false).Build();
                builder.Configuration.AddInMemoryCollection(planning.AsEnumerable()
                    .Where(entry => entry.Value is not null)
                    .Select(entry => new KeyValuePair<string, string?>("Authentication:Jarvis:Planning:" + entry.Key, entry.Value)));
            }
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (UiAddress != ApiAddress)
                    options.Listen(IPAddress.Parse(UiAddress.Host), UiAddress.Port);
                options.Listen(IPAddress.Parse(ApiAddress.Host), ApiAddress.Port, endpoint => endpoint.UseHttps(certificate));
            });
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
        }
        catch (Exception) { throw new InvalidOperationException(Invalid); }
    }

    public DbConnectionInterceptor ConnectionGuard() => new PinnedConnectionGuard(settings.ConnectionString);

    public async Task VerifyExistingSchemaAsync(IServiceProvider services)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            if (!(await db.Database.GetAppliedMigrationsAsync()).Contains("20261126001849_JarvisOpenFrontHistory") ||
                !await db.People.AsNoTracking().AnyAsync(x => x.Id == settings.PersonId && x.IsActive))
                throw new InvalidOperationException(Invalid);
            _ = await db.OpenFrontChanges.AsNoTracking().Take(1).ToListAsync();
            _ = await db.OpenFronts.AsNoTracking().Where(x => x.PersonId == settings.PersonId).Take(1).ToListAsync();
            if (services.GetRequiredService<JarvisPlanningAccess>().Enabled)
            {
                _ = await db.JarvisPlanningStates.AsNoTracking().Take(1).ToListAsync();
                _ = await db.JarvisPlanningChanges.AsNoTracking().Take(1).ToListAsync();
            }
            var certificate = services.GetRequiredService<X509Certificate2>();
            await File.WriteAllTextAsync(settings.PublicCertificatePath, certificate.ExportCertificatePem());
        }
        catch (Exception) { throw new InvalidOperationException(Invalid); }
    }

    private sealed class PinnedConnectionGuard(string approvedConnection) : DbConnectionInterceptor
    {
        private void Verify(DbConnection connection)
        {
            var actual = ParseConnection(connection.ConnectionString);
            var approved = ParseConnection(approvedConnection);
            // EF may append its client diagnostic label. It cannot change target, authentication or other options.
            actual.Remove("Application Name");
            approved.Remove("Application Name");
            if (!string.Equals(actual.ConnectionString, approved.ConnectionString, StringComparison.Ordinal))
                throw new InvalidOperationException(Invalid);
        }
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        { Verify(connection); return result; }
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        { Verify(connection); return ValueTask.FromResult(result); }
    }
}

public sealed record LocalTestSettings
{
    public string ProfileId { get; init; } = "";
    public bool ExplicitlyEnabled { get; init; }
    public bool SyntheticDataOnly { get; init; }
    public bool SelectorEnabled { get; init; }
    public bool JarvisEnabled { get; init; }
    public string ConnectionString { get; init; } = "";
    public string ConnectorId { get; init; } = "";
    public int PersonId { get; init; }
    public int[] AllowedFrontIds { get; init; } = [];
    public bool AllowAllOwnerFronts { get; init; }
    public string TokenSha256 { get; init; } = "";
    public string UiUrl { get; init; } = "";
    public string ApiUrl { get; init; } = "";
    public string PfxPath { get; init; } = "";
    public string PfxPasswordDpapiPath { get; init; } = "";
    public string PublicCertificatePath { get; init; } = "";
}
