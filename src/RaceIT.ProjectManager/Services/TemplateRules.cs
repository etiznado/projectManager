namespace RaceIT.ProjectManager.Services;

public static class TemplateRules
{
    public static string GenerateTitle(string itemTitle, string raceName, int year) => $"{itemTitle} — {raceName.Trim()} {year}";
}
