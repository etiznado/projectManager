using RaceIT.ProjectManager.Services;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Components;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("Cookies").AddCookie(options => { options.LoginPath = "/login"; options.AccessDeniedPath = "/mi-trabajo"; options.Cookie.Name = "RaceIT.DummySession"; options.ExpireTimeSpan = TimeSpan.FromHours(8); });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<WorkAccess>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ProjectManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManager")
        ?? throw new InvalidOperationException("Falta la conexión ProjectManager.")));

builder.Services.AddScoped<ActivityPlanningService>();
builder.Services.AddScoped<MilestoneService>();
builder.Services.AddScoped<BlockerService>();
builder.Services.AddScoped<TvDashboardService>();
builder.Services.AddScoped<ProjectService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapPost("/session/login", async (HttpContext context, IDbContextFactory<ProjectManagerDbContext> factory) =>
{
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    var selected = form["user"].ToString();
    var claims = new List<Claim>();
    if (selected == "admin") {
        claims.Add(new(ClaimTypes.NameIdentifier, "dummy-admin"));
        claims.Add(new(ClaimTypes.Name, "Administrador"));
        claims.Add(new(ClaimTypes.Role, "Administrador"));
    } else {
        if (!int.TryParse(selected, out var id)) return Results.BadRequest("Selecciona un usuario válido.");
        await using var db = await factory.CreateDbContextAsync();
        var person = await db.Responsibles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (person is null) return Results.BadRequest("El usuario ya no existe.");
        claims.Add(new(ClaimTypes.NameIdentifier, $"responsible:{id}"));
        claims.Add(new("ResponsibleId", id.ToString()));
        claims.Add(new(ClaimTypes.Name, person.Name));
        claims.Add(new(ClaimTypes.Role, "Empleado"));
    }
    await context.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
    return Results.LocalRedirect(selected == "admin" ? "/administracion" : "/mi-trabajo");
}).AllowAnonymous();
app.MapPost("/session/logout", async (HttpContext context) => {
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    await context.SignOutAsync("Cookies");
    return Results.LocalRedirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();






