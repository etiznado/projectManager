using System.Net;
using Microsoft.Data.SqlClient;

namespace RaceIT.ProjectManager.Services;

public static class AuthenticationGate
{
    public static bool SelectorEnabled(bool privateQaEnvironment, bool explicitlyEnabled, bool syntheticDataOnly,
        IPAddress? remoteAddress, string? connectionString) =>
        privateQaEnvironment && explicitlyEnabled && syntheticDataOnly && remoteAddress is not null &&
        IPAddress.IsLoopback(remoteAddress) && IsDedicatedQaDatabase(connectionString);

    public static bool IsDedicatedQaDatabase(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        try
        {
            var connection = new SqlConnectionStringBuilder(connectionString);
            return string.Equals(connection.InitialCatalog, "RaceIT_PM_SyntheticQA", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(connection.DataSource, @"(localdb)\RaceIT_PM_SyntheticQA", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(connection.AttachDBFilename) && string.IsNullOrEmpty(connection.FailoverPartner);
        }
        catch (ArgumentException) { return false; }
    }
}
