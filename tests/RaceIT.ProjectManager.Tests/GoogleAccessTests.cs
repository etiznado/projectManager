using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;
using RaceIT.ProjectManager.Services;

namespace RaceIT.ProjectManager.Tests;

public sealed class GoogleAccessTests
{
    private const string QaConnection = @"Server=(localdb)\RaceIT_PM_SyntheticQA;Database=RaceIT_PM_SyntheticQA;Integrated Security=True";
    private static ClaimsPrincipal GoogleClaims(string verified = "true", string email = "person@example.test", string subject = "subject-1") => new(new ClaimsIdentity(
        [new(ClaimTypes.NameIdentifier, subject), new(ClaimTypes.Email, email),
         new("google_issuer", "https://accounts.google.com"), new("google_email_verified", verified)], "GoogleTemp"));

    private static ClaimsPrincipal Admin(string source, int id = 1) => new(new ClaimsIdentity(
        [new("PersonId", id.ToString()), new(ClaimTypes.Role, "Administrador"), new("auth_source", source)], "Cookies"));

    [Fact]
    public void Google_identity_requires_verified_email_and_stable_subject()
    {
        Assert.NotNull(GoogleAccessService.ReadVerifiedIdentity(GoogleClaims()));
        Assert.Null(GoogleAccessService.ReadVerifiedIdentity(GoogleClaims("false")));
        Assert.Null(GoogleAccessService.ReadVerifiedIdentity(GoogleClaims("true", "")));
    }

    [Fact]
    public void Ordinary_sign_in_ignores_and_deletes_a_cancelled_invitation()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "RaceIT.Invitation=stale-token";

        var properties = GoogleAttemptState.StartSignIn(context);

        Assert.Contains("RaceIT.Invitation=", context.Response.Headers.SetCookie.ToString());
        Assert.True(GoogleAttemptState.TryReadCompletion(context, properties, out var invitationToken));
        Assert.Null(invitationToken);
    }

    [Fact]
    public void Acceptance_requires_the_token_from_the_same_attempt_and_clears_on_failure()
    {
        var context = new DefaultHttpContext();
        var properties = GoogleAttemptState.StartAcceptance(context, "synthetic-token-1");
        context.Request.Headers.Cookie = "RaceIT.Invitation=synthetic-token-1";
        Assert.True(GoogleAttemptState.TryReadCompletion(context, properties, out var token));
        Assert.Equal("synthetic-token-1", token);

        context.Request.Headers.Cookie = "RaceIT.Invitation=other-token";
        Assert.False(GoogleAttemptState.TryReadCompletion(context, properties, out token));
        Assert.Null(token);
        Assert.False(GoogleAttemptState.TryReadCompletion(context, new AuthenticationProperties(), out token));

        GoogleAttemptState.ClearInvitation(context);
        Assert.Contains("RaceIT.Invitation=", context.Response.Headers.SetCookie.ToString());
        Assert.Contains("expires=", context.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, true, true, "127.0.0.1", false)]
    [InlineData(true, false, true, "127.0.0.1", false)]
    [InlineData(true, true, false, "127.0.0.1", false)]
    [InlineData(true, true, true, "192.0.2.4", false)]
    [InlineData(true, true, true, "127.0.0.1", true)]
    public void Selector_requires_all_private_guards(bool privateQa, bool enabled, bool synthetic, string ip, bool expected) =>
        Assert.Equal(expected, AuthenticationGate.SelectorEnabled(privateQa, enabled, synthetic, IPAddress.Parse(ip), QaConnection));

    [Theory]
    [InlineData(@"Server=localhost\SQLEXPRESS;Database=projectManager;Integrated Security=True")]
    [InlineData(@"Server=(localdb)\RaceIT_PM_SyntheticQA;Database=projectManager;Integrated Security=True")]
    [InlineData(@"Server=localhost\SQLEXPRESS;Database=RaceIT_PM_SyntheticQA;Integrated Security=True")]
    [InlineData(@"Server=(localdb)\RaceIT_PM_SyntheticQA;Database=RaceIT_PM_SyntheticQA;AttachDbFilename=C:\Owner\projectManager.mdf;Integrated Security=True")]
    [InlineData("")]
    public void Selector_rejects_owner_or_unverified_database(string connection) =>
        Assert.False(AuthenticationGate.SelectorEnabled(true, true, true, IPAddress.Loopback, connection));

    [Fact]
    public async Task Bootstrap_acceptance_is_single_use_and_keeps_existing_role()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueBootstrapAsync(1, "person@example.test", DateTimeOffset.UtcNow.AddHours(1), "https://accounts.google.com", "subject-1");
        var identity = GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!;
        var person = await fixture.Service.AcceptAsync(token, identity);
        Assert.Equal(1, person?.Id);
        Assert.Equal(PersonRole.Admin, person?.Role);
        Assert.Null(await fixture.Service.AcceptAsync(token, identity));
        Assert.Equal(1, (await fixture.Service.FindLinkedPersonAsync(identity))?.Id);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Single(await db.GoogleIdentityLinks.ToListAsync());
        Assert.NotNull((await db.GoogleInvitations.SingleAsync()).ConsumedAtUtc);
        Assert.Equal(1, await db.AuthenticationAuditEvents.CountAsync(x => x.Action == "invitation_rejected"));
    }

    [Fact]
    public async Task Simultaneous_acceptance_never_creates_two_links()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueBootstrapAsync(1, "person@example.test", DateTimeOffset.UtcNow.AddHours(1), "https://accounts.google.com", "subject-1");
        var identity = GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!;

        var outcomes = await Task.WhenAll(
            fixture.Service.AcceptAsync(token, identity), fixture.Service.AcceptAsync(token, identity));

        Assert.Single(outcomes, x => x is not null);
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.GoogleIdentityLinks.CountAsync());
        Assert.Equal(1, await db.GoogleInvitations.CountAsync(x => x.ConsumedAtUtc != null));
        Assert.Equal(1, await db.AuthenticationAuditEvents.CountAsync(x => x.Action == "invitation_rejected"));
    }

    [Fact]
    public async Task Selector_and_inactive_admin_cannot_issue_invitation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.IssueInvitationAsync(Admin("selector"), 2, "person@example.test", DateTimeOffset.UtcNow.AddHours(1)));
        await using (var db = await fixture.Factory.CreateDbContextAsync())
        {
            var admin = await db.People.SingleAsync(x => x.Id == 1);
            admin.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.IssueInvitationAsync(Admin("google"), 2, "person@example.test", DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task Wrong_verified_email_does_not_consume_invitation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueBootstrapAsync(1, "person@example.test", DateTimeOffset.UtcNow.AddHours(1), "https://accounts.google.com", "subject-1");
        Assert.Null(await fixture.Service.AcceptAsync(token, GoogleAccessService.ReadVerifiedIdentity(GoogleClaims(email: "wrong@example.test"))!));
        Assert.NotNull(await fixture.Service.AcceptAsync(token, GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!));
    }

    [Fact]
    public async Task Expired_invitation_cannot_link()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueBootstrapAsync(1, "person@example.test", DateTimeOffset.UtcNow.AddHours(1), "https://accounts.google.com", "subject-1");
        await using (var updateDb = await fixture.Factory.CreateDbContextAsync())
        {
            (await updateDb.GoogleInvitations.SingleAsync()).ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await updateDb.SaveChangesAsync();
        }
        Assert.Null(await fixture.Service.AcceptAsync(token, GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!));
        await using var db = await fixture.Factory.CreateDbContextAsync();
        Assert.Empty(await db.GoogleIdentityLinks.ToListAsync());
    }

    [Fact]
    public async Task Existing_link_cannot_be_reassigned_to_another_person()
    {
        await using var fixture = await Fixture.CreateAsync();
        var identity = GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!;
        var firstToken = await fixture.Service.IssueBootstrapAsync(1, identity.Email, DateTimeOffset.UtcNow.AddHours(1), identity.Issuer, identity.Subject);
        Assert.NotNull(await fixture.Service.AcceptAsync(firstToken, identity));
        var secondToken = await fixture.Service.IssueInvitationAsync(Admin("google"), 2, identity.Email, DateTimeOffset.UtcNow.AddHours(1));
        Assert.Null(await fixture.Service.AcceptAsync(secondToken, identity));
        Assert.Equal(1, (await fixture.Service.FindLinkedPersonAsync(identity))?.Id);
    }

    [Fact]
    public async Task Bootstrap_rejects_same_email_with_wrong_google_subject()
    {
        await using var fixture = await Fixture.CreateAsync();
        var token = await fixture.Service.IssueBootstrapAsync(1, "person@example.test", DateTimeOffset.UtcNow.AddHours(1), "https://accounts.google.com", "subject-1");
        Assert.Null(await fixture.Service.AcceptAsync(token, GoogleAccessService.ReadVerifiedIdentity(GoogleClaims(subject: "other-subject"))!));
        Assert.NotNull(await fixture.Service.AcceptAsync(token, GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!));
    }

    [Fact]
    public async Task Bootstrap_cannot_reopen_after_linked_admin_becomes_inactive()
    {
        await using var fixture = await Fixture.CreateAsync();
        var identity = GoogleAccessService.ReadVerifiedIdentity(GoogleClaims())!;
        var token = await fixture.Service.IssueBootstrapAsync(1, identity.Email, DateTimeOffset.UtcNow.AddHours(1), identity.Issuer, identity.Subject);
        Assert.NotNull(await fixture.Service.AcceptAsync(token, identity));
        await using (var updateDb = await fixture.Factory.CreateDbContextAsync())
        {
            (await updateDb.People.SingleAsync(x => x.Id == 1)).IsActive = false;
            (await updateDb.People.SingleAsync(x => x.Id == 2)).Role = PersonRole.Admin;
            await updateDb.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.IssueBootstrapAsync(2, "other@example.test", DateTimeOffset.UtcNow.AddHours(1), identity.Issuer, "subject-2"));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;
        public IDbContextFactory<ProjectManagerDbContext> Factory { get; }
        public GoogleAccessService Service { get; }

        private Fixture(SqliteConnection connection, ServiceProvider provider)
        {
            this.connection = connection;
            this.provider = provider;
            Factory = provider.GetRequiredService<IDbContextFactory<ProjectManagerDbContext>>();
            Service = new GoogleAccessService(Factory);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddDbContextFactory<ProjectManagerDbContext>(options => options.UseSqlite(connection));
            var fixture = new Fixture(connection, services.BuildServiceProvider());
            await using var db = await fixture.Factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
            db.People.Add(new Person { Id = 2, Name = "Synthetic user", Role = PersonRole.User, IsActive = true });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
