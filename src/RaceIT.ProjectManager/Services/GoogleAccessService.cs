using System.Data;
using System.Data.Common;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RaceIT.ProjectManager.Data;
using RaceIT.ProjectManager.Domain;

namespace RaceIT.ProjectManager.Services;

public sealed record VerifiedGoogleIdentity(string Issuer, string Subject, string Email);

public static class GoogleAttemptState
{
    private const string CookieName = "RaceIT.Invitation";
    private const string PurposeKey = "RaceIT.Google.Purpose";
    private const string TokenHashKey = "RaceIT.Google.InvitationHash";
    private const string SignInPurpose = "sign-in";
    private const string AcceptancePurpose = "acceptance";

    public static AuthenticationProperties StartSignIn(HttpContext context)
    {
        ClearInvitation(context);
        return Properties(SignInPurpose);
    }

    public static AuthenticationProperties StartAcceptance(HttpContext context, string token)
    {
        ClearInvitation(context);
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10), IsEssential = true
        });
        var properties = Properties(AcceptancePurpose);
        properties.Items[TokenHashKey] = Hash(token);
        return properties;
    }

    public static bool TryReadCompletion(HttpContext context, AuthenticationProperties? properties, out string? invitationToken)
    {
        invitationToken = null;
        if (properties?.Items.TryGetValue(PurposeKey, out var purpose) != true) return false;
        if (purpose == SignInPurpose) return true;
        if (purpose != AcceptancePurpose || !properties.Items.TryGetValue(TokenHashKey, out var expectedHash)) return false;
        var token = context.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(token) || !string.Equals(Hash(token), expectedHash, StringComparison.Ordinal)) return false;
        invitationToken = token;
        return true;
    }

    public static void ClearInvitation(HttpContext context) => context.Response.Cookies.Delete(CookieName);

    private static AuthenticationProperties Properties(string purpose)
    {
        var properties = new AuthenticationProperties { RedirectUri = "/auth/google/complete" };
        properties.Items[PurposeKey] = purpose;
        return properties;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class GoogleAccessService(IDbContextFactory<ProjectManagerDbContext> factory)
{
    public static VerifiedGoogleIdentity? ReadVerifiedIdentity(ClaimsPrincipal user)
    {
        var issuer = user.FindFirst("google_issuer")?.Value;
        var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (issuer != "https://accounts.google.com" || string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(email) || user.FindFirst("google_email_verified")?.Value != "true") return null;
        return new VerifiedGoogleIdentity(issuer, subject, email);
    }

    public async Task<Person?> FindLinkedPersonAsync(VerifiedGoogleIdentity identity)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.GoogleIdentityLinks.Where(x => x.Issuer == identity.Issuer && x.Subject == identity.Subject && x.Person.IsActive)
            .Select(x => x.Person).AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task<string> IssueInvitationAsync(ClaimsPrincipal issuer, int personId, string expectedEmail, DateTimeOffset expiresAtUtc)
    {
        if (issuer.Identity?.IsAuthenticated != true || issuer.FindFirst("auth_source")?.Value != "google")
            throw new UnauthorizedAccessException();
        var issuerId = WorkAccess.PersonIdOf(issuer) ?? throw new UnauthorizedAccessException();
        return await IssueCoreAsync(personId, expectedEmail, expiresAtUtc, false, issuerId, null, null);
    }

    // Called only by the operator-only command path; no web endpoint invokes this method.
    public Task<string> IssueBootstrapAsync(int personId, string expectedEmail, DateTimeOffset expiresAtUtc,
        string expectedIssuer, string expectedSubject) =>
        IssueCoreAsync(personId, expectedEmail, expiresAtUtc, true, null, expectedIssuer, expectedSubject);

    private async Task<string> IssueCoreAsync(int personId, string expectedEmail, DateTimeOffset expiresAtUtc, bool bootstrap, int? issuerId,
        string? expectedIssuer, string? expectedSubject)
    {
        if (string.IsNullOrWhiteSpace(expectedEmail) || !System.Net.Mail.MailAddress.TryCreate(expectedEmail.Trim(), out var address) ||
            expiresAtUtc <= DateTimeOffset.UtcNow || expiresAtUtc > DateTimeOffset.UtcNow.AddDays(7) ||
            bootstrap && (expectedIssuer != "https://accounts.google.com" || string.IsNullOrWhiteSpace(expectedSubject) || expectedSubject.Length > 255))
            throw new ArgumentException("Invitación inválida.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (issuerId is int adminId && !await db.People.AnyAsync(x => x.Id == adminId && x.IsActive && x.Role == PersonRole.Admin))
            throw new UnauthorizedAccessException();
        if (!await db.People.AnyAsync(x => x.Id == personId && x.IsActive && (!bootstrap || x.Role == PersonRole.Admin)) ||
            await db.GoogleIdentityLinks.AnyAsync(x => x.PersonId == personId) ||
            bootstrap && (await db.GoogleIdentityLinks.AnyAsync(x => x.Person.IsActive && x.Person.Role == PersonRole.Admin) ||
                await db.GoogleInvitations.AnyAsync(x => x.IsBootstrap && x.ConsumedAtUtc != null)))
            throw new InvalidOperationException("No se puede emitir la invitación.");
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        db.GoogleInvitations.Add(new GoogleInvitation
        {
            PersonId = personId, ExpectedEmail = address.Address.ToLowerInvariant(),
            ExpectedIssuer = bootstrap ? expectedIssuer : null, ExpectedSubject = bootstrap ? expectedSubject : null,
            TokenHash = Hash(token),
            ExpiresAtUtc = expiresAtUtc, IssuedAtUtc = DateTimeOffset.UtcNow,
            IssuedByPersonId = issuerId, IsBootstrap = bootstrap
        });
        db.AuthenticationAuditEvents.Add(new AuthenticationAuditEvent { Action = bootstrap ? "bootstrap_issued" : "invitation_issued", PersonId = personId, AtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return token;
    }

    public async Task<Person?> AcceptAsync(string token, VerifiedGoogleIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            return await AcceptCoreAsync(token, identity);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or InvalidOperationException)
        {
            // A concurrent winner may consume the invitation or create the unique link first.
            // Only classify this as a rejected attempt after checking that persisted outcome.
            if (await RecordConcurrentRejectionIfObservedAsync(token, identity)) return null;
            throw;
        }
    }

    private async Task<Person?> AcceptCoreAsync(string token, VerifiedGoogleIdentity identity)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var invitation = await db.GoogleInvitations.Include(x => x.Person).SingleOrDefaultAsync(x => x.TokenHash == Hash(token));
        if (invitation is null || invitation.ConsumedAtUtc is not null || invitation.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            !invitation.Person.IsActive || !string.Equals(invitation.ExpectedEmail, identity.Email, StringComparison.OrdinalIgnoreCase) ||
            invitation.IsBootstrap && (invitation.ExpectedIssuer != identity.Issuer || invitation.ExpectedSubject != identity.Subject) ||
            invitation.IsBootstrap && (invitation.Person.Role != PersonRole.Admin ||
                await db.GoogleIdentityLinks.AnyAsync(x => x.Person.IsActive && x.Person.Role == PersonRole.Admin) ||
                await db.GoogleInvitations.AnyAsync(x => x.IsBootstrap && x.ConsumedAtUtc != null)) ||
            await db.GoogleIdentityLinks.AnyAsync(x => x.PersonId == invitation.PersonId || x.Issuer == identity.Issuer && x.Subject == identity.Subject))
        {
            db.AuthenticationAuditEvents.Add(new AuthenticationAuditEvent { Action = "invitation_rejected", PersonId = invitation?.PersonId, AtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return null;
        }
        invitation.ConsumedAtUtc = DateTimeOffset.UtcNow;
        db.GoogleIdentityLinks.Add(new GoogleIdentityLink { PersonId = invitation.PersonId, Issuer = identity.Issuer, Subject = identity.Subject, CreatedAtUtc = DateTimeOffset.UtcNow });
        db.AuthenticationAuditEvents.Add(new AuthenticationAuditEvent { Action = invitation.IsBootstrap ? "bootstrap_accepted" : "invitation_accepted", PersonId = invitation.PersonId, AtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return invitation.Person;
    }

    private async Task<bool> RecordConcurrentRejectionIfObservedAsync(string token, VerifiedGoogleIdentity identity)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync();
                var invitation = await db.GoogleInvitations.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == Hash(token));
                if (invitation is null) return false;
                var conflict = invitation.ConsumedAtUtc is not null || await db.GoogleIdentityLinks.AnyAsync(x =>
                    x.PersonId == invitation.PersonId || x.Issuer == identity.Issuer && x.Subject == identity.Subject);
                if (conflict)
                {
                    db.AuthenticationAuditEvents.Add(new AuthenticationAuditEvent
                    {
                        Action = "invitation_rejected", PersonId = invitation.PersonId, AtUtc = DateTimeOffset.UtcNow
                    });
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex) when (ex is DbException or DbUpdateException or InvalidOperationException)
            {
                // A competing serializable transaction can still hold the row or index lock.
            }
            if (attempt < 2) await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
        }
        return false;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
