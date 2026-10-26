using DatabaseSetup;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Foundation.PersistenceTests;

// Explicit local-only suite, excluded from the database-free generic solution/CI job.
public sealed class SchemaTests
{
    private const string A = "10000000-0000-0000-0000-000000000001";
    private const string B = "10000000-0000-0000-0000-000000000002";
    private const string U = "20000000-0000-0000-0000-000000000001";
    private const string V = "20000000-0000-0000-0000-000000000002";
    private const string Branch = "30000000-0000-0000-0000-000000000001";
    private static string Member => $"organization_id='{A}' AND user_id='{U}'";
    private static string Grant => $"INSERT INTO organizations.membership_branch_grants VALUES ('{A}','{U}','{Branch}')";
    private static string Capability => $"INSERT INTO organizations.membership_capabilities VALUES ('{A}','{U}','workspace.read',false)";

    private static async Task<NpgsqlConnection> Open()
    {
        var connection = new NpgsqlConnection(LocalDatabase.ConnectionString());
        await connection.OpenAsync();
        return connection;
    }
    private static async Task<int> Execute(NpgsqlConnection c, string sql)
        => await new NpgsqlCommand(sql, c).ExecuteNonQueryAsync();
    private static async Task<T> Scalar<T>(NpgsqlConnection c, string sql)
        => (T)(await new NpgsqlCommand(sql, c).ExecuteScalarAsync())!;
    private static async Task<long> Revision(NpgsqlConnection c)
        => await Scalar<long>(c, $"SELECT authorization_revision FROM organizations.memberships WHERE {Member}");
    private static async Task Seed(NpgsqlConnection c)
    {
        await Execute(c, $"""
            INSERT INTO identity.staff_users VALUES ('{U}',true),('{V}',true);
            INSERT INTO organizations.organizations VALUES ('{A}','Synthetic A','active'),('{B}','Synthetic B','active');
            INSERT INTO organizations.branches VALUES ('{A}','{Branch}','Synthetic branch',true),('{B}','{Branch}','Synthetic branch',true);
            INSERT INTO organizations.memberships VALUES ('{A}','{U}',1,'active');
            """);
    }
    private static async Task InFixture(Func<NpgsqlConnection, Task> test)
    {
        await using var c = await Open();
        await using var tx = await c.BeginTransactionAsync();
        await Seed(c);
        try { await test(c); }
        finally { await tx.RollbackAsync(); }
    }
    private static async Task Reject(NpgsqlConnection c, string sql, string state)
    {
        await Execute(c, "SAVEPOINT rejection");
        var error = await Assert.ThrowsAsync<PostgresException>(() => Execute(c, sql));
        Assert.Equal(state, error.SqlState);
        await Execute(c, "ROLLBACK TO SAVEPOINT rejection");
    }

    [Fact]
    public async Task ReviewedMigrationCreatesSevenTablesAndMatchesModel()
    {
        await using var context = LocalDatabase.CreateContext(LocalDatabase.ConnectionString());
        Assert.Equal(2, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        await using var c = await Open();
        Assert.Equal(7L, await Scalar<long>(c, "SELECT count(*) FROM information_schema.tables WHERE table_schema IN ('identity','organizations') AND table_type='BASE TABLE'"));
        Assert.Equal(2L, await Scalar<long>(c, "SELECT count(*) FROM organizations.capabilities"));
        Assert.Equal(6L, await Scalar<long>(c, "SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname IN ('identity','organizations') AND c.relrowsecurity AND c.relforcerowsecurity"));
        Assert.Equal(2L, await Scalar<long>(c, "SELECT count(*) FROM pg_indexes WHERE schemaname='organizations' AND indexdef LIKE '%organization_id, branch_id, user_id%' OR schemaname='organizations' AND indexdef LIKE '%user_id, organization_id%'"));
    }

    [Theory]
    [InlineData("INSERT INTO identity.staff_users VALUES ('00000000-0000-0000-0000-000000000000',true)", "23514")]
    [InlineData("INSERT INTO organizations.organizations VALUES ('10000000-0000-0000-0000-000000000003','Synthetic','unknown')", "23514")]
    [InlineData("UPDATE organizations.memberships SET status='unknown'", "23514")]
    [InlineData("UPDATE organizations.memberships SET status=NULL", "23502")]
    [InlineData("UPDATE organizations.organizations SET display_name=repeat('x',161)", "22001")]
    [InlineData("INSERT INTO organizations.capabilities VALUES ('unknown')", "23514")]
    [InlineData("INSERT INTO organizations.memberships VALUES ('10000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002',0,'active')", "23514")]
    [InlineData("INSERT INTO organizations.memberships VALUES ('10000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002',2,'active')", "23514")]
    public Task LifecycleAndValueConstraints(string sql, string state)
        => InFixture(c => Reject(c, sql, state));

    [Fact]
    public Task CompositeForeignKeysRejectCrossTenantAssociations()
        => InFixture(async c => {
            // Branch identifier exists in B too, but no membership for this actor in B.
            await Reject(c, $"INSERT INTO organizations.membership_branch_grants VALUES ('{B}','{U}','{Branch}')", "23503");
            await Reject(c, $"INSERT INTO organizations.membership_capabilities VALUES ('{B}','{U}','workspace.read',false)", "23503");
            await Execute(c, $"DELETE FROM organizations.branches WHERE organization_id='{A}'");
            await Reject(c, Grant, "23503"); // A's membership cannot use the branch in B.
            await Reject(c, $"INSERT INTO organizations.memberships VALUES ('{A}','40000000-0000-0000-0000-000000000001',1,'active')", "23503");
            await Reject(c, $"INSERT INTO organizations.memberships VALUES ('40000000-0000-0000-0000-000000000001','{V}',1,'active')", "23503");
            Assert.Equal(1L, await Revision(c));
        });

    [Fact]
    public Task RestrictiveDeletesAndTenantLocalUniqueness()
        => InFixture(async c => {
            await Execute(c, Grant);
            await Execute(c, Capability);
            Assert.Equal(2L, await Scalar<long>(c, $"SELECT count(*) FROM organizations.branches WHERE branch_id='{Branch}'"));
            await Reject(c, $"DELETE FROM identity.staff_users WHERE user_id='{U}'", "23001");
            await Reject(c, $"DELETE FROM organizations.organizations WHERE organization_id='{A}'", "23001");
            await Reject(c, $"DELETE FROM organizations.branches WHERE organization_id='{A}'", "23001");
            await Reject(c, Grant, "23505");
            await Reject(c, Capability, "23505");
            await Reject(c, $"INSERT INTO organizations.memberships VALUES ('{A}','{U}',1,'active')", "23505");
            await Reject(c, $"INSERT INTO organizations.membership_capabilities VALUES ('{A}','{U}','delegation.scope.evaluate',true)", "23514");
        });

    [Fact]
    public Task EveryGrantMutationAndLifecycleTransitionBumpsRevision()
        => InFixture(async c => {
            Assert.Equal(1L, await Revision(c));
            await Execute(c, Grant); Assert.Equal(2L, await Revision(c));
            await Execute(c, $"UPDATE organizations.membership_branch_grants SET branch_id=branch_id WHERE {Member}");
            Assert.Equal(3L, await Revision(c));
            await Execute(c, Capability); Assert.Equal(4L, await Revision(c));
            await Execute(c, $"UPDATE organizations.membership_capabilities SET is_delegable=true WHERE {Member}");
            Assert.Equal(5L, await Revision(c));
            await Execute(c, $"DELETE FROM organizations.membership_capabilities WHERE {Member}");
            Assert.Equal(6L, await Revision(c));
            await Execute(c, $"DELETE FROM organizations.membership_branch_grants WHERE {Member}");
            Assert.Equal(7L, await Revision(c));
            foreach (var state in new[] {"suspended", "removed", "active"})
                await Execute(c, $"UPDATE organizations.memberships SET status='{state}' WHERE {Member}");
            Assert.Equal(10L, await Revision(c));
            Assert.Equal(0, await Execute(c, $"UPDATE organizations.memberships SET status='suspended' WHERE {Member} AND authorization_revision=1"));
        });

    [Theory]
    [InlineData("UPDATE organizations.memberships SET authorization_revision=0")]
    [InlineData("UPDATE organizations.memberships SET authorization_revision=99")]
    [InlineData("UPDATE organizations.memberships SET user_id='20000000-0000-0000-0000-000000000002'")]
    [InlineData("DELETE FROM organizations.memberships")]
    [InlineData("TRUNCATE organizations.memberships CASCADE")]
    [InlineData("TRUNCATE organizations.membership_branch_grants")]
    [InlineData("TRUNCATE organizations.membership_capabilities")]
    [InlineData("UPDATE organizations.branches SET organization_id='10000000-0000-0000-0000-000000000003'")]
    public Task RevisionHistoryCannotBeReset(string sql)
        => InFixture(c => Reject(c, sql, "23514"));

    [Fact]
    public Task GrantOwnershipCannotMove()
        => InFixture(async c => {
            await Execute(c, Grant); await Execute(c, Capability);
            await Execute(c, $"INSERT INTO organizations.memberships VALUES ('{B}','{U}',1,'active')");
            await Reject(c, $"UPDATE organizations.membership_branch_grants SET organization_id='{B}'", "23514");
            await Reject(c, $"UPDATE organizations.membership_capabilities SET capability_code='delegation.scope.evaluate'", "23514");
        });

    [Fact]
    public Task SavepointRollbackRestoresRevisionAndGrantRows()
        => InFixture(async c => {
            await Execute(c, "SAVEPOINT changes");
            await Execute(c, Grant); await Execute(c, Capability);
            await Execute(c, "UPDATE organizations.memberships SET status='removed'");
            Assert.Equal(4L, await Revision(c));
            await Execute(c, "ROLLBACK TO SAVEPOINT changes");
            Assert.Equal(1L, await Revision(c));
            Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.membership_branch_grants"));
            Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.membership_capabilities"));
        });

    [Fact]
    public async Task FullTransactionRollbackLeavesNoSyntheticUsers()
    {
        var id = Guid.NewGuid();
        await using var c = await Open();
        await using (var tx = await c.BeginTransactionAsync())
        {
            await using var cmd = new NpgsqlCommand("INSERT INTO identity.staff_users VALUES ($1,true)", c);
            cmd.Parameters.AddWithValue(id); await cmd.ExecuteNonQueryAsync();
            await tx.RollbackAsync();
        }
        await using var check = new NpgsqlCommand("SELECT count(*) FROM identity.staff_users WHERE user_id=$1", c);
        check.Parameters.AddWithValue(id);
        Assert.Equal(0L, (long)(await check.ExecuteScalarAsync())!);
    }

    [Fact]
    public Task RevisionOverflowFailsAtomically()
        => InFixture(async c => {
            // Privileged test setup only; rolled back with the surrounding fixture.
            await Execute(c, "ALTER TABLE organizations.memberships DISABLE TRIGGER membership_revision");
            await Execute(c, "UPDATE organizations.memberships SET authorization_revision=9223372036854775807");
            await Execute(c, "ALTER TABLE organizations.memberships ENABLE TRIGGER membership_revision");
            await Reject(c, Grant, "22003");
            Assert.Equal(long.MaxValue, await Revision(c));
            Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.membership_branch_grants"));
        });

    [Fact]
    public Task SetupWriteProbeCannotEscalateOrAssumeMaintainer()
        => InFixture(async c => {
            // Setup-only negative control: BYPASSRLS, never runtime evidence.
            await Execute(c, "SET LOCAL SESSION AUTHORIZATION qr_setup");
            Assert.True(await Scalar<bool>(c, "SELECT session_user = current_user AND NOT (SELECT rolsuper FROM pg_roles WHERE rolname=current_user)"));
            await Execute(c, Grant); await Execute(c, Capability);
            await Execute(c, "UPDATE organizations.memberships SET status='suspended'");
            Assert.Equal(4L, await Revision(c));
            await Reject(c, "UPDATE organizations.memberships SET authorization_revision=1", "42501");
            await Reject(c, "ALTER TABLE organizations.memberships DISABLE TRIGGER membership_revision", "42501");
            await Reject(c, "SET ROLE qr_revision_maintainer", "42501");
            await Reject(c, "SELECT organizations.bump_grant_revision()", "42501");
            await Reject(c, "CREATE TABLE organizations.shadow(id int)", "42501");
            await Execute(c, "RESET SESSION AUTHORIZATION");
            Assert.True(await Scalar<bool>(c, "SELECT NOT rolcanlogin AND NOT rolsuper AND NOT rolbypassrls AND NOT rolcreaterole AND NOT rolcreatedb FROM pg_roles WHERE rolname='qr_revision_maintainer'"));
            Assert.True(await Scalar<bool>(c, "SELECT p.prosecdef AND p.proowner='qr_revision_maintainer'::regrole AND p.proconfig=ARRAY['search_path=pg_catalog'] FROM pg_proc p WHERE p.oid='organizations.bump_grant_revision()'::regprocedure"));
            Assert.False(await Scalar<bool>(c, "SELECT has_schema_privilege('qr_revision_maintainer','organizations','CREATE')"));
        });
    [Fact]
    public async Task EfConcurrencyTokenRejectsStaleMembershipRevision()
    {
        await using var c = await Open();
        await using var tx = await c.BeginTransactionAsync();
        await Seed(c);
        await using var context = new FoundationDbContext(new DbContextOptionsBuilder<FoundationDbContext>().UseNpgsql(c).Options);
        await context.Database.UseTransactionAsync(tx);
        var membership = await context.Set<Dictionary<string, object>>("Membership").SingleAsync();
        Assert.Equal(1L, membership["authorization_revision"]);
        await Execute(c, "UPDATE organizations.memberships SET status='suspended'");
        membership["status"] = "removed";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
        Assert.Equal(2L, await Revision(c));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task FullRollbackRestoresMembershipAndGrants()
    {
        await using var c = await Open();
        await using (var tx = await c.BeginTransactionAsync())
        {
            await Seed(c); await Execute(c, Grant); await Execute(c, Capability);
            Assert.Equal(3L, await Revision(c));
            await tx.RollbackAsync();
        }
        Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.memberships"));
        Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.membership_branch_grants"));
        Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM organizations.membership_capabilities"));
    }
}
