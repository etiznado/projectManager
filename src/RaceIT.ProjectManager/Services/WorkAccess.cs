using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RaceIT.ProjectManager.Services;

public enum WorkPermission { ReadOwnWork, ManageOwnWork, ManageProjects, ReadAll, ManageUsers }

public sealed class WorkAccess(AuthenticationStateProvider authentication)
{
    public async Task<ClaimsPrincipal> UserAsync() => (await authentication.GetAuthenticationStateAsync()).User;
    public static bool Allows(ClaimsPrincipal user, WorkPermission permission) =>
        user.Identity?.IsAuthenticated == true && (user.IsInRole("Administrador") ||
            (user.IsInRole("Empleado") && permission is WorkPermission.ReadOwnWork or WorkPermission.ManageOwnWork));
    public async Task RequireAsync(WorkPermission permission)
    {
        if (!Allows(await UserAsync(), permission)) throw new ArgumentException("No tienes permiso para esta operación.");
    }
    public async Task<bool> IsAdminAsync() => Allows(await UserAsync(), WorkPermission.ManageProjects);
    public async Task<int?> PersonIdAsync() => int.TryParse((await UserAsync()).FindFirstValue("ResponsibleId"), out var id) ? id : null;
    public async Task RequireActivityAsync(int? responsibleId)
    {
        await RequireAsync(WorkPermission.ManageOwnWork);
        if (!await IsAdminAsync() && (responsibleId is null || responsibleId != await PersonIdAsync()))
            throw new ArgumentException("La actividad no está asignada a ti.");
    }
}
