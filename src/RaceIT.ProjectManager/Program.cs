using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Components;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("Cookies").AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/mi-semana";
    options.Cookie.Name = "RaceIT.Session";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<WorkAccess>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ProjectManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManager")
        ?? throw new InvalidOperationException("Falta la conexión ProjectManager.")));

builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<WorkRecordService>();
builder.Services.AddScoped<WeeklyPlanService>();
builder.Services.AddScoped<PanoramaService>();
builder.Services.AddScoped<TemplateService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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
    if (!int.TryParse(form["user"], out var id)) return Results.BadRequest("Selecciona un usuario válido.");
    await using var db = await factory.CreateDbContextAsync();
    var person = await db.People.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
    if (person is null) return Results.BadRequest("El usuario ya no existe.");
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, $"person:{id}"),
        new("PersonId", id.ToString()),
        new(ClaimTypes.Name, person.Name),
        new(ClaimTypes.Role, person.Role == PersonRole.Admin ? "Administrador" : "Usuario")
    };
    await context.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
    return Results.LocalRedirect(person.Role == PersonRole.Admin ? "/administracion" : "/mi-semana");
}).AllowAnonymous();

app.MapPost("/session/logout", async (HttpContext context) =>
{
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    await context.SignOutAsync("Cookies");
    return Results.LocalRedirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("DataSeeder");
    try
    {
        await DataSeeder.SeedAsync(app.Services);
        logger.LogInformation("Datos de prueba sembrados correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo sembrar los datos de prueba.");
    }
}

app.Run();
