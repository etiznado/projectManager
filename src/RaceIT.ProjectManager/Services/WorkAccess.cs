using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public enum WorkPermission { ReadOwnWork, ManageOwnWork, ManageProjects, ReadAll, ManageUsers }

public sealed class WorkAccess(AuthenticationStateProvider authentication, IDbContextFactory<ProjectManagerDbContext> factory)
{
    public async Task<ClaimsPrincipal> UserAsync()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        var id = PersonIdOf(user);
        if (user.Identity?.IsAuthenticated != true || id is null || user.FindFirst("auth_source")?.Value is not ("google" or "selector"))
            return new ClaimsPrincipal(new ClaimsIdentity());
        await using var db = await factory.CreateDbContextAsync();
        var person = await db.People.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (person is null) return new ClaimsPrincipal(new ClaimsIdentity());
        var claims = new List<Claim>
        {
            new("PersonId", person.Id.ToString()),
            new(ClaimTypes.NameIdentifier, $"person:{person.Id}"),
            new(ClaimTypes.Name, person.Name),
            new(ClaimTypes.Role, person.Role == PersonRole.Admin ? "Administrador" : "Usuario"),
            new("auth_source", user.FindFirst("auth_source")!.Value)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }

    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Administrador");
    public static int? PersonIdOf(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("PersonId")?.Value, out var id) ? id : null;
    public static bool CanCreateActivityInProject(bool isAdmin, bool ownWorkOnly, int? activityPersonId, int? projectPersonId) =>
        isAdmin && !ownWorkOnly || activityPersonId is not null && activityPersonId == projectPersonId;
    public static bool CanEditProject(bool isAdmin, int? currentPersonId, int? projectPersonId) =>
        isAdmin || currentPersonId is not null && currentPersonId == projectPersonId;
    public static bool Allows(ClaimsPrincipal user, WorkPermission permission) =>
        user.Identity?.IsAuthenticated == true &&
        (IsAdmin(user) ||
         (user.IsInRole("Usuario") && permission is WorkPermission.ReadOwnWork or WorkPermission.ManageOwnWork));

    public async Task<bool> IsAuthenticatedAsync() => (await UserAsync()).Identity?.IsAuthenticated == true;
    public async Task<bool> IsAdminAsync() => IsAdmin(await UserAsync());
    public async Task<int?> PersonIdAsync() => PersonIdOf(await UserAsync());

    public async Task RequireAuthenticatedAsync()
    {
        if (!await IsAuthenticatedAsync()) throw new UnauthorizedAccessException("Inicia sesión para continuar.");
    }

    public async Task RequireAdminAsync()
    {
        if (!await IsAdminAsync()) throw new UnauthorizedAccessException("Esta operación requiere permisos de administrador.");
    }

    public async Task RequireOwnAsync(int? personId)
    {
        await RequireAuthenticatedAsync();
        if (await IsAdminAsync()) return;
        var current = await PersonIdAsync();
        if (personId is null || current is null || current != personId)
            throw new UnauthorizedAccessException("El trabajo no está asignado a ti.");
    }
}
