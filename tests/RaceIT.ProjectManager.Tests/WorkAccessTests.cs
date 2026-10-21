using System.Security.Claims;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class WorkAccessTests
{
    private static ClaimsPrincipal Admin() => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Role, "Administrador"),
        new Claim("PersonId", "1")
    ], "test"));

    private static ClaimsPrincipal User(int personId) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Role, "Usuario"),
        new Claim("PersonId", personId.ToString())
    ], "test"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    [Fact]
    public void IsAdmin_true_for_admin() => Assert.True(WorkAccess.IsAdmin(Admin()));

    [Fact]
    public void IsAdmin_false_for_user() => Assert.False(WorkAccess.IsAdmin(User(5)));

    [Fact]
    public void PersonIdOf_parses_claim() => Assert.Equal(7, WorkAccess.PersonIdOf(User(7)));

    [Fact]
    public void PersonIdOf_null_when_missing() => Assert.Null(WorkAccess.PersonIdOf(Anonymous()));

    [Fact]
    public void Allows_admin_has_any_permission() => Assert.True(WorkAccess.Allows(Admin(), WorkPermission.ManageUsers));

    [Fact]
    public void Allows_user_can_manage_own_work() => Assert.True(WorkAccess.Allows(User(1), WorkPermission.ManageOwnWork));

    [Fact]
    public void Allows_user_cannot_manage_projects() => Assert.False(WorkAccess.Allows(User(1), WorkPermission.ManageProjects));

    [Fact]
    public void Allows_anonymous_is_denied() => Assert.False(WorkAccess.Allows(Anonymous(), WorkPermission.ReadOwnWork));
}
