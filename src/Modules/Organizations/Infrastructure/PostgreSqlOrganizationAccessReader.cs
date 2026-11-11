using Npgsql;
using Organizations.Application;
using Organizations.Domain;
using PostgreSql.Infrastructure;

namespace Organizations.Infrastructure;

/// <summary>Materializes only Organizations-owned data in the caller's shared synthetic session.</summary>
public sealed class PostgreSqlOrganizationAccessReader(ReadOnlyTenantSession session) : IOrganizationAccessReader
{
    public async ValueTask<OrganizationAccessSnapshot?> ReadCurrentAsync(Guid userId, Guid organizationId,
        Guid branchId, CancellationToken cancellationToken)
    {
        if (!await session.MatchesScopeAsync(userId, organizationId, branchId, cancellationToken)) return null;

        await using var command = new NpgsqlCommand("""
            SELECT m.status, m.authorization_revision, o.organization_id, o.status,
                   b.branch_id, b.organization_id, b.is_enabled,
                   ARRAY(SELECT g.branch_id FROM organizations.membership_branch_grants g
                         WHERE g.organization_id=$1 AND g.user_id=$2),
                   ARRAY(SELECT c.capability_code FROM organizations.membership_capabilities c
                         WHERE c.organization_id=$1 AND c.user_id=$2),
                   ARRAY(SELECT c.capability_code FROM organizations.membership_capabilities c
                         WHERE c.organization_id=$1 AND c.user_id=$2 AND c.is_delegable)
            FROM organizations.memberships m
            LEFT JOIN organizations.organizations o ON o.organization_id=m.organization_id
            LEFT JOIN organizations.branches b ON b.organization_id=m.organization_id AND b.branch_id=$3
            WHERE m.organization_id=$1 AND m.user_id=$2
            """, session.Connection, session.Transaction);
        command.Parameters.AddWithValue(organizationId);
        command.Parameters.AddWithValue(userId);
        command.Parameters.AddWithValue(branchId);
        await using var rows = await command.ExecuteReaderAsync(cancellationToken);
        if (!await rows.ReadAsync(cancellationToken)) return null;

        MembershipStatus? membershipStatus = rows.GetString(0) switch
        {
            "active" => MembershipStatus.Active,
            "suspended" => MembershipStatus.Suspended,
            "removed" => MembershipStatus.Removed,
            _ => null
        };
        var revision = rows.GetInt64(1);
        if (membershipStatus is null || revision < 1) return null;

        PharmacyOrganization? organization = null;
        if (!rows.IsDBNull(2))
        {
            OrganizationStatus? organizationStatus = rows.GetString(3) switch
            {
                "active" => OrganizationStatus.Active,
                "suspended" => OrganizationStatus.Suspended,
                "closed" => OrganizationStatus.Closed,
                _ => null
            };
            if (organizationStatus is null) return null;
            organization = new(rows.GetGuid(2), organizationStatus.Value);
        }
        Branch? branch = rows.IsDBNull(4) ? null : new(rows.GetGuid(4), rows.GetGuid(5), rows.GetBoolean(6));
        var branchIds = rows.GetFieldValue<Guid[]>(7);
        var capabilities = MapCapabilities(rows.GetFieldValue<string[]>(8));
        var delegable = MapCapabilities(rows.GetFieldValue<string[]>(9));
        if (branchIds.Contains(Guid.Empty) || capabilities is null || delegable is null) return null;
        var membership = new Membership(userId, organizationId, membershipStatus.Value, revision,
            branchIds, capabilities, delegable);
        cancellationToken.ThrowIfCancellationRequested();
        return new(organization, branch, membership);
    }

    private static Capability[]? MapCapabilities(string[] codes)
    {
        var result = new Capability[codes.Length];
        for (var index = 0; index < codes.Length; index++)
        {
            Capability? capability = codes[index] switch
            {
                "workspace.read" => Capability.WorkspaceRead,
                "delegation.scope.evaluate" => Capability.DelegationScopeEvaluate,
                _ => null
            };
            if (capability is null) return null;
            result[index] = capability.Value;
        }
        return result;
    }
}
