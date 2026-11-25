using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class ThemeStateTests
{
    [Fact]
    public void StartsDarkAndTogglesBothWays()
    {
        var theme = new ThemeState();

        Assert.True(theme.IsDark);
        theme.Toggle();
        Assert.False(theme.IsDark);
        theme.Toggle();
        Assert.True(theme.IsDark);
        Assert.True(new ThemeState().IsDark);
    }
}
