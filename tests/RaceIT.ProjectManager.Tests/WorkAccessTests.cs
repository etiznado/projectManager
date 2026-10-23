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

    [Theory]
    [InlineData(true, true, 1, 2, false)]
    [InlineData(true, true, 1, 1, true)]
    [InlineData(false, false, 1, 2, false)]
    [InlineData(false, false, 1, 1, true)]
    [InlineData(true, false, 1, 2, true)]
    public void Activity_project_policy_distinguishes_own_work_from_global_admin(
        bool isAdmin, bool ownWorkOnly, int activityPersonId, int projectPersonId, bool expected) =>
        Assert.Equal(expected, WorkAccess.CanCreateActivityInProject(isAdmin, ownWorkOnly, activityPersonId, projectPersonId));

    [Theory]
    [InlineData(false, 1, 1, true)]
    [InlineData(false, 1, 2, false)]
    [InlineData(false, 1, null, false)]
    [InlineData(true, 1, 2, true)]
    public void Project_edit_policy_checks_owner_or_admin(bool isAdmin, int currentPersonId, int? projectPersonId, bool expected) =>
        Assert.Equal(expected, WorkAccess.CanEditProject(isAdmin, currentPersonId, projectPersonId));
}
