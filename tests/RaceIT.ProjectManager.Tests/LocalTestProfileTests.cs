using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
}
