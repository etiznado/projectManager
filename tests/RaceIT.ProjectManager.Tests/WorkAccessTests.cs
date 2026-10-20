using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public class WorkAccessTests
{
    private static ClaimsPrincipal User(string role, int? id = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (id.HasValue) claims.Add(new("ResponsibleId", id.Value.ToString()));
        return new(new ClaimsIdentity(claims, "test"));
    }
    [Fact]
    public void AnonymousHasNoPermissions()
    {
        foreach (var permission in Enum.GetValues<WorkPermission>())
            Assert.False(WorkAccess.Allows(new ClaimsPrincipal(new ClaimsIdentity()), permission));
    }
    [Fact]
    public void AdministratorHasFivePermissions()
    {
        foreach (var permission in Enum.GetValues<WorkPermission>())
            Assert.True(WorkAccess.Allows(User("Administrador"), permission));
    }
    [Fact]
    public void EmployeeHasOnlyOwnWorkPermissions()
    {
        var user = User("Empleado", 7);
        Assert.True(WorkAccess.Allows(user, WorkPermission.ReadOwnWork));
        Assert.True(WorkAccess.Allows(user, WorkPermission.ManageOwnWork));
        Assert.False(WorkAccess.Allows(user, WorkPermission.ManageProjects));
        Assert.False(WorkAccess.Allows(user, WorkPermission.ReadAll));
        Assert.False(WorkAccess.Allows(user, WorkPermission.ManageUsers));
    }
    [Fact]
    public async Task AssignmentControlsEditEvenForUnassignedWork()
    {
        var access = new WorkAccess(new Session(User("Empleado", 7)));
        await access.RequireActivityAsync(7);
        await Assert.ThrowsAsync<ArgumentException>(() => access.RequireActivityAsync(8));
        await Assert.ThrowsAsync<ArgumentException>(() => access.RequireActivityAsync(null));
    }
    [Fact]
    public async Task MissingPersonDoesNotGrantAccessToUnassignedWork()
    {
        var access = new WorkAccess(new Session(User("Empleado")));
        await Assert.ThrowsAsync<ArgumentException>(() => access.RequireActivityAsync(null));
    }
    private sealed class Session(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }
}
