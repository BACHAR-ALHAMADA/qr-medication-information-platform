using System.Collections.Frozen;
using System.Text;
using Identity.Application;
using Npgsql;

namespace Identity.Infrastructure;

/// <summary>Exact Identity-owned lookup, usable only in the explicitly privileged synthetic session.</summary>
public sealed class PostgreSqlExternalStaffIdentityResolver : IExternalStaffIdentityResolver
{
    private readonly SyntheticIdentityMappingSession session;
    private readonly FrozenSet<string> approvedIssuers;

    public PostgreSqlExternalStaffIdentityResolver(SyntheticIdentityMappingSession session, IEnumerable<string> approvedIssuers)
    {
        this.session = session;
        this.approvedIssuers = approvedIssuers.ToFrozenSet(StringComparer.Ordinal);
    }

    public async ValueTask<ExternalStaffIdentityResolution?> ResolveAsync(ValidatedAuthentication authentication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authentication);
        cancellationToken.ThrowIfCancellationRequested();
        var key = authentication.Principal;
        var missing = new ExternalStaffIdentityResolution(key, ExternalIdentityResolutionStatus.Missing, null);
        if (!session.IsActive || !approvedIssuers.Contains(key.Issuer)
            || Encoding.UTF8.GetByteCount(key.Issuer) > 1024 || Encoding.UTF8.GetByteCount(key.Subject) > 1024)
            return missing;

        await using var command = new NpgsqlCommand("""
            SELECT u.user_id, u.is_enabled
            FROM identity.authentication_issuers i
            JOIN identity.external_staff_identities m ON m.issuer_id=i.issuer_id
            JOIN identity.staff_users u ON u.user_id=m.user_id
            WHERE i.issuer=$1 COLLATE "C" AND m.subject=$2 COLLATE "C"
                AND i.is_enabled AND m.state='active' AND u.is_enabled
            """, session.Connection, session.Transaction);
        command.Parameters.AddWithValue(key.Issuer);
        command.Parameters.AddWithValue(key.Subject);
        await using var rows = await command.ExecuteReaderAsync(cancellationToken);
        if (!await rows.ReadAsync(cancellationToken)) return missing;
        var staff = new StaffIdentity(rows.GetGuid(0), rows.GetBoolean(1));
        if (await rows.ReadAsync(cancellationToken))
            return new(key, ExternalIdentityResolutionStatus.Ambiguous, null);
        cancellationToken.ThrowIfCancellationRequested();
        return staff.UserId == Guid.Empty ? missing : new(key, ExternalIdentityResolutionStatus.Unique, staff);
    }
}
