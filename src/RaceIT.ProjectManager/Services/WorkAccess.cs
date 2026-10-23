using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RaceIT.ProjectManager.Services;

public enum WorkPermission { ReadOwnWork, ManageOwnWork, ManageProjects, ReadAll, ManageUsers }

public sealed class WorkAccess(AuthenticationStateProvider authentication)
{
    public async Task<ClaimsPrincipal> UserAsync() => (await authentication.GetAuthenticationStateAsync()).User;

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
