namespace RaceIT.ProjectManager.Services;

public sealed class ThemeState
{
    public bool IsDark { get; private set; }

    public void Toggle() => IsDark = !IsDark;
}
