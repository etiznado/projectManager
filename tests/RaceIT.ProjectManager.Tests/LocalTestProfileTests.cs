using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class LocalTestProfileTests
{
    private static IConfiguration Configuration(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build();

    private static LocalTestSettings Settings() => new()
    {
        ProfileId = "synthetic-test-only", ExplicitlyEnabled = true, SyntheticDataOnly = true,
        SelectorEnabled = true, JarvisEnabled = true,
        ConnectionString = @"Server=localhost\FixtureOnly;Database=SyntheticOnly;Integrated Security=True;Encrypt=True;TrustServerCertificate=True",
        ConnectorId = "connector-test-only", PersonId = 1, AllowedFrontIds = [1], TokenSha256 = new string('a', 64),
        UiUrl = "http://127.0.0.1:15001", ApiUrl = "https://127.0.0.1:15002",
        PfxPath = Path.Combine(Path.GetTempPath(), "profile-test.pfx"),
        PfxPasswordDpapiPath = Path.Combine(Path.GetTempPath(), "profile-test.dpapi"),
        PublicCertificatePath = Path.Combine(Path.GetTempPath(), "profile-test.pem")
    };

    [Fact]
    public void Owner_wide_mode_requires_typed_matching_private_JSON_and_defaults_to_restricted()
    {
        var directory = Path.Combine(Path.GetTempPath(), "owner-mode-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var runtimePath = Path.Combine(directory, "runtime.json");
        var approvalPath = Path.Combine(directory, "approval.json");
        var configuration = Configuration(new() { ["LocalTestProfile:Enabled"] = "true", ["LocalTestProfile:Directory"] = directory });
        var original = JsonSerializer.SerializeToNode(Settings())!.AsObject();
        original.Remove("AllowAllOwnerFronts");
        string Json(string? mode)
        {
            var node = original.DeepClone().AsObject();
            if (mode is not null) node["AllowAllOwnerFronts"] = System.Text.Json.Nodes.JsonNode.Parse(mode);
            return node.ToJsonString();
        }
        try
        {
            foreach (var (runtime, approval) in new[] { ((string?)null, (string?)null), ("false", "false"), ("true", "true"), (null, "false"), ("false", null) })
            {
                File.WriteAllText(runtimePath, Json(runtime)); File.WriteAllText(approvalPath, Json(approval));
                Assert.NotNull(LocalTestProfile.Load("PrivateQa", configuration));
            }
            foreach (var (runtime, approval) in new[] { ("true", (string?)null), (null, "true"), ("true", "false"), ("false", "true"), ("\"true\"", "true"), ("true", "\"true\""), ("null", "true"), ("true", "null"), ("0", "false") })
            {
                File.WriteAllText(runtimePath, Json(runtime)); File.WriteAllText(approvalPath, Json(approval));
                Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Load("PrivateQa", configuration));
            }
        }
        finally { File.Delete(runtimePath); File.Delete(approvalPath); Directory.Delete(directory); }
        var malformedConnector = new Dictionary<string, string?> {
            ["Authentication:Jarvis:Enabled"] = "true", ["Authentication:Jarvis:ConnectorId"] = "synthetic",
            ["Authentication:Jarvis:PersonId"] = "1", ["Authentication:Jarvis:TokenSha256"] = new string('a', 64),
            ["Authentication:Jarvis:AllowAllOwnerFronts"] = "all"
        };
        Assert.Throws<InvalidOperationException>(() => JarvisConnectorAccess.Read(Configuration(malformedConnector)));
    }

    [Fact]
    public void Approved_owner_wide_mode_propagates_to_effective_connector_configuration_without_changing_grants()
    {
        var directory = Path.Combine(Path.GetTempPath(), "owner-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var settings = Settings() with { PfxPath = Path.Combine(directory, "synthetic.pfx"), PfxPasswordDpapiPath = Path.Combine(directory, "synthetic.dpapi") };
        var password = Guid.NewGuid().ToString("N");
        var previous = Environment.GetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD");
        using var key = RSA.Create(2048);
        using var certificate = new CertificateRequest("CN=synthetic-profile-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        File.WriteAllBytes(settings.PfxPath, certificate.Export(X509ContentType.Pfx, password));
        File.WriteAllBytes(settings.PfxPasswordDpapiPath, []);
        try
        {
            foreach (var enabled in new[] { false, true })
            {
                var selected = settings with { AllowAllOwnerFronts = enabled };
                var profile = LocalTestProfile.Validate("PrivateQa", selected, selected, Configuration());
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "PrivateQa" });
                Environment.SetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD", password);
                profile.Configure(builder);
                using var services = builder.Services.BuildServiceProvider();
                _ = services.GetRequiredService<X509Certificate2>();
                var access = JarvisConnectorAccess.Read(builder.Configuration);
                Assert.Equal(enabled, access.AllowAllOwnerFronts);
                Assert.Equal(selected.PersonId, access.PersonId);
                Assert.Equal(selected.ConnectorId, access.ConnectorId);
                Assert.Equal("1", builder.Configuration["Authentication:Jarvis:AllowedFrontIds:0"]);
                Assert.Null(Environment.GetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD"));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD", previous);
            File.Delete(settings.PfxPath); File.Delete(settings.PfxPasswordDpapiPath); Directory.Delete(directory);
        }
    }

    [Fact]
    public void Unrequested_profile_does_not_load_private_settings_and_incomplete_activation_cannot_fall_back()
    {
        Assert.Null(LocalTestProfile.Load("Development", Configuration()));
        var exception = Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Load("PrivateQa", Configuration(new()
        { ["LocalTestProfile:Directory"] = Path.GetTempPath(), ["LocalTestProfile:Enabled"] = "false" })));
        Assert.DoesNotContain(Path.GetTempPath(), exception.Message);
        Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Load("PrivateQa", Configuration(new()
        { ["LocalTestProfile:Enabled"] = "true" })));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("")]
    public void Explicit_flags_never_allow_a_profile_outside_PrivateQa(string environment) =>
        Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate(environment, Settings(), Settings(), Configuration()));

    [Fact]
    public void Each_required_runtime_or_independent_approval_flag_is_enforced()
    {
        var changes = new[]
        {
            Settings() with { ExplicitlyEnabled = false }, Settings() with { SyntheticDataOnly = false },
            Settings() with { SelectorEnabled = false }, Settings() with { JarvisEnabled = false }
        };
        foreach (var settings in changes)
        {
            Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate("PrivateQa", settings, Settings(), Configuration()));
            Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate("PrivateQa", Settings(), settings, Configuration()));
        }
    }

    [Fact]
    public void Runtime_target_identity_grants_digest_and_asset_paths_cannot_replace_the_independent_pin()
    {
        var changes = new[]
        {
            Settings() with { ProfileId = "another-fixture" },
            Settings() with { ConnectionString = @"Server=localhost\FixtureOnly;Database=Other;Integrated Security=True" },
            Settings() with { ConnectorId = "another-connector" }, Settings() with { PersonId = 2 },
            Settings() with { AllowedFrontIds = [1, 2] }, Settings() with { TokenSha256 = new string('b', 64) },
            Settings() with { PfxPath = Path.Combine(Path.GetTempPath(), "other.pfx") },
            Settings() with { PublicCertificatePath = Path.Combine(Path.GetTempPath(), "other.pem") }
        };
        foreach (var settings in changes)
            Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate("PrivateQa", settings, Settings(), Configuration()));
    }

    [Theory]
    [InlineData("http://0.0.0.0:15001", "https://127.0.0.1:15002")]
    [InlineData("http://192.0.2.5:15001", "https://127.0.0.1:15002")]
    [InlineData("http://127.0.0.1:15001", "http://127.0.0.1:15002")]
    [InlineData("http://127.0.0.1:15001", "https://127.0.0.1:15001")]
    public void Even_matching_pins_cannot_authorize_external_or_non_TLS_API_bindings(string ui, string api)
    {
        var settings = Settings() with { UiUrl = ui, ApiUrl = api };
        Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate("PrivateQa", settings, settings, Configuration()));
    }

    [Theory]
    [InlineData("urls", "http://0.0.0.0:9999")]
    [InlineData("http_ports", "9999")]
    [InlineData("https_ports", "9998")]
    [InlineData("Kestrel:Endpoints:Extra:Url", "http://0.0.0.0:9999")]
    public void Endpoint_configuration_or_environment_cannot_add_listeners(string key, string value) =>
        Assert.Throws<InvalidOperationException>(() => LocalTestProfile.Validate("PrivateQa", Settings(), Settings(), Configuration(new() { [key] = value })));

    [Fact]
    public void Approved_profile_login_is_loopback_only_and_legacy_guard_remains_unchanged()
    {
        var settings = Settings();
        var profile = LocalTestProfile.Validate("PrivateQa", settings, settings, Configuration());
        Assert.True(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Loopback, settings.ConnectionString, profile));
        Assert.False(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Parse("192.0.2.8"), settings.ConnectionString, profile));
        Assert.False(AuthenticationGate.SelectorEnabled(false, true, true, IPAddress.Loopback, settings.ConnectionString, profile));
        Assert.False(AuthenticationGate.SelectorEnabled(true, false, true, IPAddress.Loopback, settings.ConnectionString, profile));
        Assert.False(AuthenticationGate.SelectorEnabled(true, true, false, IPAddress.Loopback, settings.ConnectionString, profile));
        Assert.False(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Loopback, settings.ConnectionString));
        Assert.False(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Loopback, "Server=localhost;Database=Other;Integrated Security=True", profile));
        Assert.True(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Loopback,
            @"Server=(localdb)\RaceIT_PM_SyntheticQA;Database=RaceIT_PM_SyntheticQA;Integrated Security=True"));
    }

    [Fact]
    public void Connection_guard_rejects_retargeting_before_opening_any_SQL_connection()
    {
        var settings = Settings();
        var profile = LocalTestProfile.Validate("PrivateQa", settings, settings, Configuration());
        using var valid = new SqlConnection(settings.ConnectionString);
        profile.ConnectionGuard().ConnectionOpening(valid, null!, default);
        using var other = new SqlConnection("Server=localhost;Database=Other;Integrated Security=True");
        Assert.Throws<InvalidOperationException>(() => profile.ConnectionGuard().ConnectionOpening(other, null!, default));
    }

    [Fact]
    public void Provider_diagnostic_label_is_allowed_but_target_and_other_options_remain_pinned()
    {
        var settings = Settings();
        var profile = LocalTestProfile.Validate("PrivateQa", settings, settings, Configuration());
        var provider = new SqlConnectionStringBuilder(settings.ConnectionString) { ApplicationName = "Provider diagnostic label" };
        using var labeled = new SqlConnection(provider.ConnectionString);
        profile.ConnectionGuard().ConnectionOpening(labeled, null!, default);
        provider.ApplicationIntent = ApplicationIntent.ReadOnly;
        using var changedOptions = new SqlConnection(provider.ConnectionString);
        Assert.Throws<InvalidOperationException>(() => profile.ConnectionGuard().ConnectionOpening(changedOptions, null!, default));
        provider.ApplicationIntent = ApplicationIntent.ReadWrite;
        provider.InitialCatalog = "DifferentSyntheticTarget";
        using var changedTarget = new SqlConnection(provider.ConnectionString);
        Assert.Throws<InvalidOperationException>(() => profile.ConnectionGuard().ConnectionOpening(changedTarget, null!, default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Configured_profile_binds_only_approved_shared_TLS_or_legacy_listeners(bool shared)
    {
        using var fixture = new PrivateProfileFiles();
        var apiPort = AvailablePort();
        var uiPort = shared ? apiPort : AvailablePort();
        while (!shared && uiPort == apiPort) uiPort = AvailablePort();
        var selected = fixture.Settings with
        {
            UiUrl = $"{(shared ? "https" : "http")}://127.0.0.1:{uiPort}",
            ApiUrl = $"https://127.0.0.1:{apiPort}"
        };
        fixture.WriteSettings(selected);
        var profile = LocalTestProfile.Load("PrivateQa", fixture.Configuration)!;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "PrivateQa" });
        fixture.Configure(profile, builder);
        await using var app = builder.Build();
        app.MapGet("/login", () => "synthetic login");
        try
        {
            await app.StartAsync();
            var bound = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
            Assert.Equal(shared ? 1 : 2, bound.Count);
            Assert.Contains(selected.ApiUrl, bound);
            Assert.Contains(selected.UiUrl, bound);
            var trustedCertificate = fixture.Certificate.GetCertHashString(HashAlgorithmName.SHA256);
            using var trusted = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    certificate?.GetCertHashString(HashAlgorithmName.SHA256) == trustedCertificate
            });
            Assert.Equal("synthetic login", await trusted.GetStringAsync(selected.ApiUrl + "/login"));
            Assert.Equal("synthetic login", await trusted.GetStringAsync(selected.UiUrl + "/login"));
            using var untrusted = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => false });
            await Assert.ThrowsAsync<HttpRequestException>(() => untrusted.GetAsync(selected.ApiUrl + "/login"));
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public void Real_planning_JSON_cannot_escape_prefix_or_change_pinned_profile_values()
    {
        using var fixture = new PrivateProfileFiles();
        File.WriteAllText(fixture.PlanningPath, """
        {
          "Enabled": true,
          "Plans": [{"Id":"synthetic-plan","ApprovalReference":"synthetic approval"}],
          "ConnectionStrings": {"ProjectManager":"forged target"},
          "TokenSha256":"forged verifier",
          "PersonId":99,
          "AllowedFrontIds":[99],
          "AllowAllOwnerFronts":true,
          "UiUrl":"http://0.0.0.0:9",
          "ApiUrl":"http://0.0.0.0:9",
          "urls":"http://0.0.0.0:9",
          "Kestrel":{"Endpoints":{"Extra":{"Url":"http://0.0.0.0:9"}}},
          "Authentication":{"Jarvis":{"TokenSha256":"forged nested verifier","PersonId":99}}
        }
        """);
        var profile = LocalTestProfile.Load("PrivateQa", fixture.Configuration)!;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "PrivateQa" });
        fixture.Configure(profile, builder);
        using var services = builder.Services.BuildServiceProvider();
        _ = services.GetRequiredService<X509Certificate2>();
        var connector = JarvisConnectorAccess.Read(builder.Configuration);
        Assert.True(JarvisPlanningAccess.Read(builder.Configuration, connector).Enabled);
        Assert.Equal(fixture.Settings.ConnectionString, builder.Configuration.GetConnectionString("ProjectManager"));
        Assert.Equal(fixture.Settings.TokenSha256, builder.Configuration["Authentication:Jarvis:TokenSha256"]);
        Assert.Equal(fixture.Settings.PersonId, connector.PersonId);
        Assert.Equal(fixture.Settings.ConnectorId, connector.ConnectorId);
        Assert.False(connector.AllowAllOwnerFronts);
        Assert.Equal("1", builder.Configuration["Authentication:Jarvis:AllowedFrontIds:0"]);
        Assert.Null(builder.Configuration["Authentication:Jarvis:AllowedFrontIds:1"]);
        Assert.Null(builder.Configuration["urls"]);
        Assert.Empty(builder.Configuration.GetSection("Kestrel").GetChildren());
        Assert.Equal(new Uri(fixture.Settings.UiUrl), profile.UiAddress);
        Assert.Equal(new Uri(fixture.Settings.ApiUrl), profile.ApiAddress);
        Assert.Equal("forged target", builder.Configuration["Authentication:Jarvis:Planning:ConnectionStrings:ProjectManager"]);
        Assert.Equal("forged nested verifier", builder.Configuration["Authentication:Jarvis:Planning:Authentication:Jarvis:TokenSha256"]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("{}", false)]
    [InlineData("{\"Enabled\":false}", false)]
    [InlineData("{\"Enabled\":true}", true)]
    [InlineData("{\"Enabled\":\"invalid\"}", true)]
    public void Missing_or_invalid_private_planning_configuration_never_grants_access(string? json, bool throws)
    {
        using var fixture = new PrivateProfileFiles();
        if (json is not null) File.WriteAllText(fixture.PlanningPath, json);
        var profile = LocalTestProfile.Load("PrivateQa", fixture.Configuration)!;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "PrivateQa" });
        fixture.Configure(profile, builder);
        using var services = builder.Services.BuildServiceProvider();
        _ = services.GetRequiredService<X509Certificate2>();
        var connector = JarvisConnectorAccess.Read(builder.Configuration);
        if (throws) Assert.Throws<InvalidOperationException>(() => JarvisPlanningAccess.Read(builder.Configuration, connector));
        else Assert.False(JarvisPlanningAccess.Read(builder.Configuration, connector).Enabled);
    }

    [Fact]
    public void Malformed_private_planning_JSON_rejects_configuration_without_exposing_private_paths()
    {
        using var fixture = new PrivateProfileFiles();
        File.WriteAllText(fixture.PlanningPath, "{not-valid-json");
        var profile = LocalTestProfile.Load("PrivateQa", fixture.Configuration)!;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "PrivateQa" });
        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Configure(profile, builder));
        using var services = builder.Services.BuildServiceProvider();
        _ = services.GetService<X509Certificate2>();
        Assert.DoesNotContain(fixture.DirectoryPath, exception.Message);
    }

    [Theory]
    [InlineData(false, "both", true)]
    [InlineData(true, "state", false)]
    [InlineData(true, "changes", false)]
    [InlineData(true, "none", true)]
    public async Task Schema_preflight_requires_enabled_planning_tables_without_repair_or_migration(bool enabled, string missing, bool succeeds)
    {
        using var fixture = new PrivateProfileFiles();
        var profile = LocalTestProfile.Validate("PrivateQa", fixture.Settings, fixture.Settings, Configuration());
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Jarvis:Enabled"] = "true", ["Authentication:Jarvis:ConnectorId"] = fixture.Settings.ConnectorId,
            ["Authentication:Jarvis:PersonId"] = "1", ["Authentication:Jarvis:TokenSha256"] = fixture.Settings.TokenSha256,
            ["Authentication:Jarvis:Planning:Enabled"] = enabled.ToString(),
            ["Authentication:Jarvis:Planning:Plans:0:Id"] = "synthetic-plan", ["Authentication:Jarvis:Planning:Plans:0:ApprovalReference"] = "synthetic approval"
        }).Build();
        var services = new ServiceCollection();
        services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton(JarvisPlanningAccess.Read(configuration, JarvisConnectorAccess.Read(configuration)));
        services.AddSingleton(fixture.Certificate);
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            db.OpenFronts.Add(new OpenFront { Id = 1, PersonId = 1, Title = "Synthetic preserved front" });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory VALUES ('20261126001849_JarvisOpenFrontHistory','10.0.13');");
            if (missing is "state" or "both") await db.Database.ExecuteSqlRawAsync("DROP TABLE JarvisPlanningStates;");
            if (missing is "changes" or "both") await db.Database.ExecuteSqlRawAsync("DROP TABLE JarvisPlanningChanges;");
        }
        async Task<string> DatabaseSnapshot()
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name,sql FROM sqlite_master WHERE type IN ('table','index') ORDER BY name;";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(reader.GetString(0) + "|" + (reader.IsDBNull(1) ? "" : reader.GetString(1)));
            return string.Join('\n', rows);
        }
        var schema = await DatabaseSnapshot();
        if (succeeds) await profile.VerifyExistingSchemaAsync(provider);
        else
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => profile.VerifyExistingSchemaAsync(provider));
            Assert.DoesNotContain(fixture.DirectoryPath, exception.Message);
            Assert.False(File.Exists(fixture.Settings.PublicCertificatePath));
        }
        Assert.Equal(schema, await DatabaseSnapshot());
        await using var check = await factory.CreateDbContextAsync();
        Assert.Equal("Synthetic preserved front", Assert.Single(await check.OpenFronts.ToListAsync()).Title);
        Assert.Equal(new[] { "20261126001849_JarvisOpenFrontHistory" }, await check.Database.GetAppliedMigrationsAsync());
    }

    private static int AvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class PrivateProfileFiles : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "feat019-profile-" + Guid.NewGuid().ToString("N"));
        public LocalTestSettings Settings { get; }
        public X509Certificate2 Certificate { get; }
        public string PlanningPath => Path.Combine(DirectoryPath, "planning.json");
        public IConfiguration Configuration => LocalTestProfileTests.Configuration(new() { ["LocalTestProfile:Enabled"] = "true", ["LocalTestProfile:Directory"] = DirectoryPath });
        private readonly string password = Guid.NewGuid().ToString("N");
        public PrivateProfileFiles()
        {
            Directory.CreateDirectory(DirectoryPath);
            Settings = LocalTestProfileTests.Settings() with
            {
                PfxPath = Path.Combine(DirectoryPath, "synthetic.pfx"), PfxPasswordDpapiPath = Path.Combine(DirectoryPath, "synthetic.dpapi"),
                PublicCertificatePath = Path.Combine(DirectoryPath, "synthetic.pem")
            };
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=synthetic-profile-only", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build());
            Certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            File.WriteAllBytes(Settings.PfxPath, Certificate.Export(X509ContentType.Pfx, password));
            File.WriteAllBytes(Settings.PfxPasswordDpapiPath, []);
            WriteSettings(Settings);
        }
        public void WriteSettings(LocalTestSettings settings)
        {
            File.WriteAllText(Path.Combine(DirectoryPath, "runtime.json"), JsonSerializer.Serialize(settings));
            File.WriteAllText(Path.Combine(DirectoryPath, "approval.json"), JsonSerializer.Serialize(settings));
        }
        public void Configure(LocalTestProfile profile, WebApplicationBuilder builder)
        {
            var previous = Environment.GetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD");
            try { Environment.SetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD", password); profile.Configure(builder); }
            finally { Environment.SetEnvironmentVariable("RACEIT_LOCAL_TEST_PFX_PASSWORD", previous); }
        }
        public void Dispose()
        {
            Certificate.Dispose();
            foreach (var file in new[] { "runtime.json", "approval.json", "planning.json", "synthetic.pfx", "synthetic.dpapi", "synthetic.pem" })
                File.Delete(Path.Combine(DirectoryPath, file));
            Directory.Delete(DirectoryPath);
        }
    }
}
