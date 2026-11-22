using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class ThemeStateTests
{
    [Fact]
    public void StartsLightAndTogglesBothWays()
    {
        var theme = new ThemeState();

        Assert.False(theme.IsDark);
        theme.Toggle();
        Assert.True(theme.IsDark);
        theme.Toggle();
        Assert.False(theme.IsDark);
        Assert.False(new ThemeState().IsDark);
    }
}
