using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaceIT.ProjectManager.Services;

/// <summary>Operator-owned approval evidence. Never populated by an API request.</summary>
public sealed class JarvisPlanningAccess
{
    public bool Enabled { get; private init; }
    internal HashSet<int> OwnedInitiativeIds { get; private init; } = [];
    internal IReadOnlyDictionary<string, PlanningPlan> Plans { get; private init; } = new Dictionary<string, PlanningPlan>();
    internal IReadOnlyDictionary<string, PlanningApproval> Approvals { get; private init; } = new Dictionary<string, PlanningApproval>();

    public static JarvisPlanningAccess Read(IConfiguration configuration, JarvisConnectorAccess connector)
    {
        var section = configuration.GetSection("Authentication:Jarvis:Planning");
        if (!section.GetValue<bool>("Enabled")) return new();
        var owned = section.GetSection("OwnedInitiativeIds").Get<int[]>() ?? [];
        var plans = section.GetSection("Plans").Get<PlanningPlan[]>() ?? [];
        var approvals = section.GetSection("Approvals").Get<PlanningApproval[]>() ?? [];
        if (!connector.Enabled || !ValidIds(owned) || plans.Length == 0 ||
            plans.Any(p => !Id(p.Id) || !Reference(p.ApprovalReference) || !ValidIds(p.InitiativeIds) ||
                !ValidIds(p.ProjectIds) || !ValidIds(p.ActivityIds) || p.AllowIndependentProjects && !p.AllowCreateProjects ||
                p.AllowLooseActivities && !p.AllowCreateActivities) ||
            plans.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != plans.Length ||
            approvals.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count() != approvals.Length ||
            approvals.Any(a => !Id(a.Id) || !Reference(a.ApprovalReference) || !plans.Any(p => p.Id == a.PlanId) ||
                a.PersonId != connector.PersonId || !Kinds.Contains(a.EntityKind) || a.EntityId <= 0 ||
                !Operations.Contains(a.Operation) || a.Operation == "dependencies" && a.EntityKind != "activity" ||
                a.Operation is "close" or "target-date" && a.EntityKind == "activity" ||
                a.ExpectedRevision < 0 || a.ExpectedRevision == long.MaxValue || !Hash(a.ExpectedFingerprint) || !Hash(a.ValuesSha256)))
            throw new InvalidOperationException("La configuración privada de planificación de Jarvis no es válida.");
        return new()
        {
            Enabled = true, OwnedInitiativeIds = owned.ToHashSet(),
            Plans = plans.ToDictionary(p => p.Id, StringComparer.Ordinal),
            Approvals = approvals.ToDictionary(a => a.Id, StringComparer.Ordinal)
        };
    }

    internal static readonly string[] Kinds = ["initiative", "project", "activity"];
    internal static readonly string[] Operations = ["target-date", "dependencies", "close"];
    private static bool Id(string? value) => value is { Length: > 0 and <= 200 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    private static bool Reference(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 2000;
    private static bool ValidIds(int[]? ids) => ids is not null && ids.All(x => x > 0) && ids.Distinct().Count() == ids.Length;
    internal static bool Hash(string? value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);

    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        Converters = { new CanonicalDecimalConverter() }
    };

    // Database providers may materialize 8, 8.0 or 8.00 for the same decimal value.
    // Their representation must not invalidate an otherwise current fingerprint.
    internal static string SerializeValues(object value) => JsonSerializer.Serialize(value, CanonicalOptions);

    // Public deterministic helper lets the private operator calculate exact approval payloads offline.
    public static string ValuesHash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SerializeValues(value))));

    private sealed class CanonicalDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetDecimal();
        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteRawValue(value.ToString("G29", CultureInfo.InvariantCulture));
    }
}

// Binding types are not exposed by routes. Access retains its own deserialized copies.
public sealed class PlanningPlan
{
    public string Id { get; set; } = "";
    public string ApprovalReference { get; set; } = "";
    public int[] InitiativeIds { get; set; } = [];
    public int[] ProjectIds { get; set; } = [];
    public int[] ActivityIds { get; set; } = [];
    public bool AllowCreateInitiatives { get; set; }
    public bool AllowCreateProjects { get; set; }
    public bool AllowCreateActivities { get; set; }
    public bool AllowIndependentProjects { get; set; }
    public bool AllowLooseActivities { get; set; }
}

public sealed class PlanningApproval
{
    public string Id { get; set; } = "";
    public string ApprovalReference { get; set; } = "";
    public string PlanId { get; set; } = "";
    public int PersonId { get; set; }
    public string EntityKind { get; set; } = "";
    public int EntityId { get; set; }
    public string Operation { get; set; } = "";
    public long ExpectedRevision { get; set; }
    public string ExpectedFingerprint { get; set; } = "";
    public string ValuesSha256 { get; set; } = "";
}
