using Identity.Application;
using Npgsql;
using PostgreSql.Infrastructure;

namespace Identity.Infrastructure;

/// <summary>Local synthetic reader. The caller owns the shared session and its lifetime.</summary>
public sealed class PostgreSqlStaffIdentityReader(ReadOnlyTenantSession session) : IStaffIdentityReader
{
    public async ValueTask<StaffIdentity?> ReadCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await session.MatchesScopeAsync(userId, session.OrganizationId, session.BranchId, cancellationToken))
            return null;

        await using var command = new NpgsqlCommand("""
            SELECT user_id, is_enabled FROM identity.staff_users WHERE user_id=$1
            """, session.Connection, session.Transaction);
        command.Parameters.AddWithValue(userId);
        await using var rows = await command.ExecuteReaderAsync(cancellationToken);
        if (!await rows.ReadAsync(cancellationToken)) return null;
        cancellationToken.ThrowIfCancellationRequested();
        return new StaffIdentity(rows.GetGuid(0), rows.GetBoolean(1));
    }
}
