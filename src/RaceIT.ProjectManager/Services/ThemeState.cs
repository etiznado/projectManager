namespace RaceIT.ProjectManager.Services;

public sealed class ThemeState
{
    public bool IsDark { get; private set; } = true;

    public void Toggle() => IsDark = !IsDark;
}
