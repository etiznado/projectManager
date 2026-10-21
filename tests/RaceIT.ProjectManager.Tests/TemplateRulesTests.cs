using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class TemplateRulesTests
{
    [Fact]
    public void GenerateTitle_includes_race_name_and_year()
    {
        Assert.Equal("Revisar loggers — Carrera X 2026", TemplateRules.GenerateTitle("Revisar loggers", "Carrera X", 2026));
    }

    [Fact]
    public void GenerateTitle_trims_race_name()
    {
        Assert.Equal("Preparar auto — Baja 2026", TemplateRules.GenerateTitle("Preparar auto", "  Baja  ", 2026));
    }
}
