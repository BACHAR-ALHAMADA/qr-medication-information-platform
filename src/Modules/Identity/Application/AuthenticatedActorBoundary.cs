namespace Identity.Application;

/// <summary>An exact external identity key. Constructing a key does not authenticate anyone.</summary>
public sealed record ExternalPrincipalKey
{
    public string Issuer { get; }
    public string Subject { get; }

    public ExternalPrincipalKey(string issuer, string subject)
    {
        if (string.IsNullOrWhiteSpace(issuer) || !Uri.TryCreate(issuer, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0
            || uri.Query.Length != 0 || uri.Fragment.Length != 0
            || issuer != issuer.Trim() || issuer.Any(char.IsControl)
            || string.IsNullOrWhiteSpace(subject) || subject.Any(char.IsControl))
            throw new ArgumentException("Invalid external identity key.");
        // Preserve exact strings: no case folding, trimming, email matching or issuer normalization.
        Issuer = issuer;
        Subject = subject;
    }

    public override string ToString() => nameof(ExternalPrincipalKey);
}

/// <summary>
/// Evidence supplied only by a trusted authentication adapter after protocol validation.
/// No production adapter or public issuance method exists in this synthetic slice.
/// </summary>
public sealed class ValidatedAuthentication
{
    public ExternalPrincipalKey Principal { get; }
    internal ValidatedAuthentication(ExternalPrincipalKey principal)
        => Principal = principal ?? throw new ArgumentNullException(nameof(principal));
    public override string ToString() => nameof(ValidatedAuthentication);
}

/// <summary>
/// Trusted, invocation-scoped source, not a request DTO or ClaimsPrincipal.IsAuthenticated wrapper.
/// A future adapter must validate the approved provider's protocol and session before issuing evidence.
/// </summary>
public interface IValidatedAuthenticationSource
{
    ValueTask<ValidatedAuthentication?> ReadCurrentAsync(CancellationToken cancellationToken);
}

public enum ExternalIdentityResolutionStatus { Missing, Unique, Ambiguous }

/// <summary>Trusted resolver output; the echoed principal detects mismatched lookup results.</summary>
public sealed record ExternalStaffIdentityResolution(
    ExternalPrincipalKey Principal, ExternalIdentityResolutionStatus Status, StaffIdentity? Staff)
{
    public override string ToString() => nameof(ExternalStaffIdentityResolution);
}

/// <summary>
/// Identity owns exact issuer/subject mapping and current staff lookup. No auto-provisioning,
/// email fallback, tenant inference or persistence implementation is provided here.
/// </summary>
public interface IExternalStaffIdentityResolver
{
    ValueTask<ExternalStaffIdentityResolution?> ResolveAsync(
        ValidatedAuthentication authentication, CancellationToken cancellationToken);
}

/// <summary>Resolved actor identity only; conveys no organization, branch or capability authority.</summary>
public sealed class AuthenticatedActorContext
{
    public Guid UserId { get; }
    internal AuthenticatedActorContext(Guid userId) => UserId = userId;
    public override string ToString() => nameof(AuthenticatedActorContext);
}

/// <summary>
/// Provider-neutral coordination boundary. Both dependencies are trusted composition, not user input.
/// No shared current-actor state or authentication/mapping cache is kept.
/// </summary>
public sealed class AuthenticatedActorBoundary(
    IValidatedAuthenticationSource authenticationSource, IExternalStaffIdentityResolver identities)
{
    public async ValueTask<AuthenticatedActorContext?> ResolveCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authentication = await authenticationSource.ReadCurrentAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (authentication is null) return null;

        var resolution = await identities.ResolveAsync(authentication, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (resolution is null || resolution.Status != ExternalIdentityResolutionStatus.Unique
            || resolution.Principal != authentication.Principal
            || resolution.Staff is not { IsEnabled: true } staff || staff.UserId == Guid.Empty)
            return null;

        return new AuthenticatedActorContext(staff.UserId);
    }
}
