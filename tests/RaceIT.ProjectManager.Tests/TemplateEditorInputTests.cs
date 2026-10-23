using System.ComponentModel.DataAnnotations;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class TemplateEditorInputTests
{
    [Fact]
    public void Empty_template_cannot_be_saved() => Assert.False(IsValid(new TemplateEditorInput { Name = "Carrera", Items = [] }));

    [Fact]
    public void Template_requires_a_name() => Assert.False(IsValid(new TemplateEditorInput
    {
        Items = [new TemplateItemEditInput { Title = "Preparar", EstimatedHours = 1m }]
    }));

    [Theory]
    [InlineData("", 1)]
    [InlineData("Actividad", 0)]
    public void Item_requires_title_and_positive_hours(string title, decimal hours) =>
        Assert.False(IsValid(new TemplateEditorInput
        {
            Name = "Carrera",
            Items = [new TemplateItemEditInput { Title = title, EstimatedHours = hours }]
        }));

    [Fact]
    public void Existing_and_new_items_can_be_saved_together() =>
        Assert.True(IsValid(new TemplateEditorInput
        {
            Name = "Carrera",
            Items =
            [
                new TemplateItemEditInput { Id = 3, Title = "Preparar", EstimatedHours = 2 },
                new TemplateItemEditInput { Title = "Revisar", EstimatedHours = 1.5m }
            ]
        }));

    private static bool IsValid(TemplateEditorInput input) =>
        Validator.TryValidateObject(input, new ValidationContext(input), [], true);
}
