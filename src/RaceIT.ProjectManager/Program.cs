using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Components;
using RaceIT.ProjectManager.Api;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

var builder = WebApplication.CreateBuilder(args);
var localTestProfile = LocalTestProfile.Load(builder.Environment.EnvironmentName, builder.Configuration);
localTestProfile?.Configure(builder);
if (localTestProfile is not null) builder.Services.AddSingleton(localTestProfile);
if (builder.Environment.IsEnvironment("PrivateQa"))
{
    builder.WebHost.UseStaticWebAssets();
}
var googleEnabled = builder.Configuration.GetValue<bool>("Authentication:Google:Enabled") &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]);
var selectorEnabled = builder.Environment.IsEnvironment("PrivateQa") &&
    builder.Configuration.GetValue<bool>("Authentication:Selector:Enabled") &&
    builder.Configuration.GetValue<bool>("Authentication:Selector:SyntheticDataOnly") &&
    (AuthenticationGate.IsDedicatedQaDatabase(builder.Configuration.GetConnectionString("ProjectManager")) || localTestProfile is not null);

var authentication = builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/mi-semana";
    options.Cookie.Name = localTestProfile is null ? "RaceIT.Session" : LocalTestProfile.CookieName;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnValidatePrincipal = async context =>
    {
        var id = WorkAccess.PersonIdOf(context.Principal!);
        var factory = context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        var person = id is int personId ? await db.People.AsNoTracking().SingleOrDefaultAsync(x => x.Id == personId && x.IsActive) : null;
        if (person is null || context.Principal!.FindFirst("auth_source")?.Value is not ("google" or "selector") ||
            context.Principal.FindFirst("auth_source")?.Value == "google" && !googleEnabled ||
            context.Principal.FindFirst("auth_source")?.Value == "selector" && !SelectorAllowed(context.HttpContext, selectorEnabled))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync("Cookies");
            return;
        }
        if (context.Principal.IsInRole("Administrador") != (person.Role == PersonRole.Admin))
        {
            context.ReplacePrincipal(CreatePrincipal(person, context.Principal.FindFirst("auth_source")!.Value));
            context.ShouldRenew = true;
        }
    };
});
authentication.AddCookie("GoogleTemp", options =>
{
    options.Cookie.Name = "RaceIT.Google.Temp";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});
if (googleEnabled)
{
    authentication.AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = "GoogleTemp";
        options.CallbackPath = "/signin-google";
        options.UserInformationEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        options.Scope.Add("openid");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.Events.OnCreatingTicket = context =>
        {
            if (context.User.TryGetProperty("email_verified", out var verified) && verified.ValueKind == System.Text.Json.JsonValueKind.True)
            {
                context.Identity!.AddClaim(new Claim("google_email_verified", "true"));
                context.Identity.AddClaim(new Claim("google_issuer", "https://accounts.google.com"));
            }
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            GoogleAttemptState.ClearInvitation(context.HttpContext);
            context.Response.Redirect("/login?google=failed");
            return Task.CompletedTask;
        };
    });
}
builder.Services.AddAuthorization();
builder.Services.AddJarvisConnector(builder.Configuration);
builder.Services.AddJarvisPlanning(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<WorkAccess>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<GoogleAccessService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ProjectManagerDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManager")
        ?? throw new InvalidOperationException("Falta la conexión ProjectManager."));
    if (localTestProfile is not null) options.AddInterceptors(localTestProfile.ConnectionGuard());
});

builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<WorkRecordService>();
builder.Services.AddScoped<WeeklyPlanService>();
builder.Services.AddScoped<PanoramaService>();
builder.Services.AddScoped<InitiativeDeliveryService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<OpenFrontService>();

var app = builder.Build();
if (localTestProfile is not null) await localTestProfile.VerifyExistingSchemaAsync(app.Services);

if (args.Contains("--issue-bootstrap-invitation"))
{
    if (!googleEnabled || !int.TryParse(Environment.GetEnvironmentVariable("RACEIT_BOOTSTRAP_PERSON_ID"), out var personId) ||
        !DateTimeOffset.TryParse(Environment.GetEnvironmentVariable("RACEIT_BOOTSTRAP_EXPIRES_UTC"), out var expiry))
        throw new InvalidOperationException("Falta configuración privada de bootstrap.");
    using var scope = app.Services.CreateScope();
    var token = await scope.ServiceProvider.GetRequiredService<GoogleAccessService>().IssueBootstrapAsync(
        personId, Environment.GetEnvironmentVariable("RACEIT_BOOTSTRAP_EMAIL") ?? "", expiry,
        Environment.GetEnvironmentVariable("RACEIT_BOOTSTRAP_ISSUER") ?? "",
        Environment.GetEnvironmentVariable("RACEIT_BOOTSTRAP_SUBJECT") ?? "");
    Console.Out.WriteLine(token);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (localTestProfile is null) app.UseHsts();
}
if (localTestProfile is not null)
{
    app.Use(async (context, next) =>
    {
        if (context.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip))
        { context.Response.StatusCode = 403; return; }
        await next(context);
    });
}
// API errors retain JSON and status codes; bearer credentials are never redirected over HTTP.
app.UseJarvisApiErrors();
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api/jarvis"), branch =>
{
    branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    if (localTestProfile is null) branch.UseHttpsRedirection();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapJarvisOpenFronts();
app.MapJarvisPlanning();

app.MapPost("/session/login", async (HttpContext context, IDbContextFactory<ProjectManagerDbContext> factory) =>
{
    if (!SelectorAllowed(context, selectorEnabled)) return Results.NotFound();
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    if (!int.TryParse(form["user"], out var id)) return Results.BadRequest("Selecciona un usuario válido.");
    await using var db = await factory.CreateDbContextAsync();
    var person = await db.People.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
    if (person is null) return Results.BadRequest("El usuario ya no existe.");
    await context.SignInAsync("Cookies", CreatePrincipal(person, "selector"));
    return Results.LocalRedirect(person.Role == PersonRole.Admin ? "/administracion" : "/mi-semana");
}).AllowAnonymous();

app.MapGet("/auth/google/start", (HttpContext context) =>
    googleEnabled ? Results.Challenge(GoogleAttemptState.StartSignIn(context), ["Google"]) : Results.NotFound()).AllowAnonymous();

app.MapPost("/auth/google/accept", async (HttpContext context) =>
{
    if (!googleEnabled) return Results.NotFound();
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    var token = form["invitation"].ToString();
    if (token.Length is < 32 or > 128)
    {
        GoogleAttemptState.ClearInvitation(context);
        return Results.LocalRedirect("/login?google=failed");
    }
    return Results.Challenge(GoogleAttemptState.StartAcceptance(context, token), ["Google"]);
}).AllowAnonymous();

app.MapGet("/auth/google/complete", async (HttpContext context, GoogleAccessService service) =>
{
    if (!googleEnabled) return Results.NotFound();
    var external = await context.AuthenticateAsync("GoogleTemp");
    await context.SignOutAsync("GoogleTemp");
    var validAttempt = GoogleAttemptState.TryReadCompletion(context, external.Properties, out var invitationToken);
    GoogleAttemptState.ClearInvitation(context);
    if (!validAttempt) return Results.LocalRedirect("/login?google=failed");
    var identity = external.Succeeded && external.Principal is not null ? GoogleAccessService.ReadVerifiedIdentity(external.Principal) : null;
    if (identity is null) return Results.LocalRedirect("/login?google=failed");
    Person? person = null;
    try
    {
        person = invitationToken is not null ? await service.AcceptAsync(invitationToken, identity) : await service.FindLinkedPersonAsync(identity);
    }
    catch (Exception) { return Results.LocalRedirect("/login?google=failed"); }
    if (person is null) return Results.LocalRedirect("/login?google=failed");
    await context.SignInAsync("Cookies", CreatePrincipal(person, "google"));
    return Results.LocalRedirect(person.Role == PersonRole.Admin ? "/administracion" : "/mi-semana");
}).AllowAnonymous();

app.MapPost("/auth/invitations", async (HttpContext context, GoogleAccessService service) =>
{
    if (!googleEnabled) return Results.NotFound();
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    if (!int.TryParse(form["personId"], out var personId) || !DateTimeOffset.TryParse(form["expiresUtc"], out var expiresUtc)) return Results.BadRequest();
    try
    {
        var token = await service.IssueInvitationAsync(context.User, personId, form["email"].ToString(), expiresUtc);
        return Results.Text(token, "text/plain");
    }
    catch (UnauthorizedAccessException) { return Results.Forbid(); }
    catch (ArgumentException) { return Results.BadRequest(); }
    catch (InvalidOperationException) { return Results.BadRequest(); }
}).RequireAuthorization();

app.MapPost("/session/logout", async (HttpContext context) =>
{
    await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    await context.SignOutAsync("Cookies");
    return Results.LocalRedirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using var localStopSignal = localTestProfile is null ? null : new EventWaitHandle(false, EventResetMode.ManualReset,
    $"Local\\RaceIT.ProjectManager.LocalTest.{System.Environment.ProcessId}");
var registeredStop = localStopSignal is null ? null : ThreadPool.RegisterWaitForSingleObject(localStopSignal,
    (_, _) => app.Lifetime.StopApplication(), null, Timeout.Infinite, true);
try { app.Run(); }
finally { registeredStop?.Unregister(null); }

static bool SelectorAllowed(HttpContext context, bool enabled) => enabled &&
    AuthenticationGate.SelectorEnabled(true, true, true, context.Connection.RemoteIpAddress,
        context.RequestServices.GetRequiredService<IConfiguration>().GetConnectionString("ProjectManager"),
        context.RequestServices.GetService<LocalTestProfile>());

static ClaimsPrincipal CreatePrincipal(Person person, string source) => new(new ClaimsIdentity(
    [new(ClaimTypes.NameIdentifier, $"person:{person.Id}"), new("PersonId", person.Id.ToString()),
     new(ClaimTypes.Name, person.Name), new(ClaimTypes.Role, person.Role == PersonRole.Admin ? "Administrador" : "Usuario"),
     new("auth_source", source)], "Cookies"));
