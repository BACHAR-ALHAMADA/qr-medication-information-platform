using DatabaseSetup;
using Npgsql;
using PostgreSql.Infrastructure;
using Xunit;
using static Foundation.PersistenceTests.RlsFixture;
namespace Foundation.PersistenceTests;

public sealed class RlsTests(RlsFixture fixture) : IClassFixture<RlsFixture>
{
    private Task<ReadOnlyTenantSession> Scope(Guid actor,Guid org,Guid? branch=null,NpgsqlDataSource? source=null)
        => ReadOnlyTenantSession.OpenAsync(source??fixture.Reader,TrustedSyntheticActor.FromTrustedHarness(actor),org,branch??fixture.Branch);
    private static async Task EmptyProtected(NpgsqlConnection c)
    {
        foreach(var table in new[]{"identity.staff_users","organizations.memberships","organizations.organizations","organizations.branches","organizations.membership_branch_grants","organizations.membership_capabilities"})
            Assert.Equal(0L,await Scalar<long>(c,$"SELECT count(*) FROM {table}"));
    }
    private static Task<int> Context(NpgsqlConnection c,string actor,string org,string branch)
        => Exec(c,"SELECT set_config('app.actor_id',$1,true),set_config('app.organization_id',$2,true),set_config('app.branch_id',$3,true)",actor,org,branch);

    [Fact]
    public async Task ReaderIsGenuineNonownerAndOnlyReadsCandidateTenant()
    {
        await using var scope=await Scope(fixture.Alice,fixture.A);
        var c=scope.Connection;
        Assert.True(await Scalar<bool>(c,"SELECT session_user='qr_reader' AND current_user='qr_reader'"));
        Assert.Equal("repeatable read",await Scalar<string>(c,"SHOW transaction_isolation"));
        Assert.Equal("on",await Scalar<string>(c,"SHOW transaction_read_only"));
        Assert.Equal(fixture.Alice,await Scalar<Guid>(c,"SELECT user_id FROM identity.staff_users"));
        Assert.Equal(fixture.A,await Scalar<Guid>(c,"SELECT organization_id FROM organizations.organizations"));
        Assert.Equal(1L,await Scalar<long>(c,"SELECT count(*) FROM organizations.memberships"));
        foreach(var table in new[]{"organizations.memberships","organizations.branches","organizations.membership_branch_grants","organizations.membership_capabilities"})
            Assert.Equal(0L,await Scalar<long>(c,$"SELECT count(*) FROM {table} WHERE organization_id<>$1",fixture.A));
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM identity.staff_users WHERE user_id<>$1",fixture.Alice));
        Assert.Equal(1L,await Scalar<long>(c,"SELECT count(*) FROM organizations.branches"));
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM organizations.organizations WHERE organization_id=$1",fixture.B));
        Assert.Equal(1L,await Scalar<long>(c,"SELECT count(*) FROM organizations.branches b JOIN organizations.membership_branch_grants g USING(organization_id,branch_id) JOIN organizations.memberships m USING(organization_id,user_id)"));
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM (SELECT * FROM organizations.branches) b WHERE organization_id=$1",fixture.B));
    }
    [Fact]
    public async Task ValidShapeAndTenantSelectorDoNotEstablishMembership()
    {
        await using var scope=await Scope(fixture.AOnly,fixture.B);
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.memberships"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.branches"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.organizations"));
    }
    [Theory]
    [InlineData("app.actor_id","")]
    [InlineData("app.organization_id","")]
    [InlineData("app.branch_id","")]
    [InlineData("app.actor_id","not-a-uuid")]
    [InlineData("app.organization_id","malformed")]
    [InlineData("app.branch_id","' OR true --")]
    [InlineData("app.actor_id","00000000-0000-0000-0000-000000000000")]
    [InlineData("app.organization_id","00000000-0000-0000-0000-000000000000")]
    [InlineData("app.branch_id","00000000-0000-0000-0000-000000000000")]
    public async Task MissingOrMalformedRequiredContextDeniesEveryProtectedTable(string setting,string value)
    {
        await using var scope=await Scope(fixture.Alice,fixture.A);
        await Exec(scope.Connection,"SELECT set_config($1,$2,true)",setting,value);
        await EmptyProtected(scope.Connection);
    }
    [Fact]
    public async Task NoContextHasNoProtectedRowsButSharedCatalogueRemainsReadable()
    {
        await using var c=await fixture.Reader.OpenConnectionAsync();
        await EmptyProtected(c);
        Assert.Equal(2L,await Scalar<long>(c,"SELECT count(*) FROM organizations.capabilities"));
    }
    [Fact]
    public async Task BranchSubstitutionCannotUseAnotherOrganizationsOrUnassignedBranch()
    {
        await using(var a=await Scope(fixture.Alice,fixture.A,fixture.Unassigned))
            Assert.Equal(0L,await Scalar<long>(a.Connection,"SELECT count(*) FROM organizations.branches"));
        await using(var b=await Scope(fixture.Alice,fixture.B))
        {
            Assert.Equal(fixture.B,await Scalar<Guid>(b.Connection,"SELECT organization_id FROM organizations.branches"));
            Assert.Equal(0L,await Scalar<long>(b.Connection,"SELECT count(*) FROM organizations.branches WHERE organization_id=$1",fixture.A));
        }
    }
    [Theory]
    [InlineData("suspended")]
    [InlineData("removed")]
    [InlineData("disabled-user")]
    public async Task InactiveSelfBootstrapIsVisibleButDescendantDataIsDenied(string state)
    {
        var actor=state=="suspended" ? fixture.Inactive : state=="removed" ? fixture.Removed : fixture.Disabled;
        await using var scope=await Scope(actor,fixture.A);
        Assert.Equal(1L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.memberships"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.memberships WHERE user_id<>$1",actor));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.membership_capabilities"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.membership_branch_grants"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.organizations"));
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.branches"));
    }
    [Fact]
    public async Task DisabledBranchStateIsInternalDenialMetadataNotAuthority()
    {
        await using var scope=await Scope(fixture.Alice,fixture.A,fixture.DisabledBranch);
        Assert.False(await Scalar<bool>(scope.Connection,"SELECT is_enabled FROM organizations.branches"));
    }
    [Theory]
    [InlineData("suspended")]
    [InlineData("closed")]
    public async Task InactiveOrganizationStateIsReadableForDenialButGrantsAreHidden(string state)
    {
        await fixture.Setup("UPDATE organizations.organizations SET status=$1 WHERE organization_id=$2",state,fixture.A);
        try
        {
            await using var scope=await Scope(fixture.Alice,fixture.A);
            Assert.Equal(state,await Scalar<string>(scope.Connection,"SELECT status FROM organizations.organizations"));
            Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.branches"));
            Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.membership_capabilities"));
        }
        finally { await fixture.Setup("UPDATE organizations.organizations SET status='active' WHERE organization_id=$1",fixture.A); }
    }
    [Fact]
    public async Task RepeatableReadCanPredateRevocationButNextTransactionSeesItAndStaleRevision()
    {
        await using(var before=await Scope(fixture.Alice,fixture.A))
        {
            var revision=await Scalar<long>(before.Connection,"SELECT authorization_revision FROM organizations.memberships");
            Assert.Equal(1L,await Scalar<long>(before.Connection,"SELECT count(*) FROM organizations.branches"));
            await fixture.Setup("UPDATE organizations.memberships SET status='suspended' WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice);
            try
            {
                Assert.Equal(1L,await Scalar<long>(before.Connection,"SELECT count(*) FROM organizations.branches"));
                Assert.Equal(revision,await Scalar<long>(before.Connection,"SELECT authorization_revision FROM organizations.memberships"));
                await using var after=await Scope(fixture.Alice,fixture.A);
                Assert.Equal("suspended",await Scalar<string>(after.Connection,"SELECT status FROM organizations.memberships"));
                Assert.Equal(0L,await Scalar<long>(after.Connection,"SELECT count(*) FROM organizations.branches"));
                Assert.Equal(0L,await Scalar<long>(after.Connection,"SELECT count(*) FROM organizations.memberships WHERE authorization_revision=$1",revision));
                await using var other=await Scope(fixture.Alice,fixture.B);
                Assert.Equal(1L,await Scalar<long>(other.Connection,"SELECT count(*) FROM organizations.branches"));
            }
            finally { await fixture.Setup("UPDATE organizations.memberships SET status='active' WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice); }
        }
    }
    [Fact]
    public async Task ReaderCannotWriteEvenAfterTurningOffReadOnlyTransactionDefault()
    {
        await using var c=await fixture.Reader.OpenConnectionAsync();
        await using var tx=await c.BeginTransactionAsync();
        await Exec(c,"SET TRANSACTION READ WRITE");
        foreach(var sql in new[]{"UPDATE organizations.memberships SET status='removed'","DELETE FROM organizations.membership_branch_grants","INSERT INTO organizations.capabilities VALUES ('unknown')","TRUNCATE organizations.membership_capabilities","ALTER TABLE organizations.memberships DISABLE ROW LEVEL SECURITY","SET ROLE qr_platform_owner","SET ROLE qr_revision_maintainer","SET ROLE qr_setup","SET ROLE qr_migrator","SELECT organizations.bump_grant_revision()","CREATE TABLE organizations.shadow(id int)"})
        {
            await Exec(c,"SAVEPOINT denied");
            var error=await Assert.ThrowsAsync<PostgresException>(()=>Exec(c,sql));
            Assert.Equal("42501",error.SqlState);
            await Exec(c,"ROLLBACK TO SAVEPOINT denied");
        }
        await tx.RollbackAsync();
    }
    [Fact]
    public async Task OwnerForcedRlsAndReaderFlagsAreVerifiedSeparately()
    {
        await using var reader=await fixture.Reader.OpenConnectionAsync();
        Assert.True(await Scalar<bool>(reader,"SELECT NOT rolsuper AND NOT rolbypassrls AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication FROM pg_roles WHERE rolname=current_user"));
        Assert.Equal(0L,await Scalar<long>(reader,"SELECT count(*) FROM pg_class WHERE relowner=current_user::regrole AND relnamespace IN ('identity'::regnamespace,'organizations'::regnamespace)"));
        Assert.Equal(6L,await Scalar<long>(reader,"SELECT count(*) FROM pg_class WHERE relnamespace IN ('identity'::regnamespace,'organizations'::regnamespace) AND relkind='r' AND relrowsecurity AND relforcerowsecurity"));
        Assert.False(await Scalar<bool>(reader,"SELECT has_table_privilege(current_user,'foundation.\"__EFMigrationsHistory\"','SELECT')"));
        await using var migrator=new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_migrator"));
        await migrator.OpenAsync();
        await Exec(migrator,"SET ROLE qr_platform_owner");
        Assert.Equal(0L,await Scalar<long>(migrator,"SELECT count(*) FROM organizations.memberships"));
        Assert.Equal(0L,await Scalar<long>(migrator,"SELECT count(*) FROM organizations.branches"));
        Assert.False(await Scalar<bool>(migrator,"SELECT rolbypassrls OR rolsuper FROM pg_roles WHERE rolname=current_user"));
    }
    [Fact]
    public async Task MaintainerPoliciesPermitOnlyTheExistingTriggerOperationAndRollback()
    {
        await using var c=new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_setup"));
        await c.OpenAsync();await using var tx=await c.BeginTransactionAsync();
        var old=await Scalar<long>(c,"SELECT authorization_revision FROM organizations.memberships WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice);
        await Exec(c,"UPDATE organizations.membership_capabilities SET is_delegable=NOT is_delegable WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice);
        Assert.Equal(old+1,await Scalar<long>(c,"SELECT authorization_revision FROM organizations.memberships WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice));
        await tx.RollbackAsync();
        Assert.Equal(old,await Scalar<long>(c,"SELECT authorization_revision FROM organizations.memberships WHERE organization_id=$1 AND user_id=$2",fixture.A,fixture.Alice));
    }
    [Fact]
    public async Task ContextForgeryIsAnExplicitThreatLimitNotAnAuthenticationClaim()
    {
        await using var scope=await Scope(fixture.AOnly,fixture.B);
        Assert.Equal(0L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.branches"));
        await Context(scope.Connection,fixture.Bob.ToString(),fixture.B.ToString(),fixture.Branch.ToString());
        Assert.Equal(1L,await Scalar<long>(scope.Connection,"SELECT count(*) FROM organizations.branches"));
    }
    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("error")]
    [InlineData("cancellation")]
    public async Task SamePhysicalConnectionIsCleanAfterEveryExit(string exit)
    {
        var settings=new NpgsqlConnectionStringBuilder(LocalDatabase.RoleConnectionString("qr_reader"))
            { MaxPoolSize=1,MinPoolSize=1,ApplicationName="qr-pool-reuse-"+exit };
        await using var pool=NpgsqlDataSource.Create(settings.ConnectionString);
        int pid;
        await using(var first=await Scope(fixture.Alice,fixture.A,source:pool))
        {
            pid=first.Connection.ProcessID;
            Assert.Equal(fixture.A,await Scalar<Guid>(first.Connection,"SELECT organization_id FROM organizations.branches"));
            if(exit=="commit") await first.CommitAsync();
            else if(exit=="error")
            {
                var error=await Assert.ThrowsAsync<PostgresException>(()=>Exec(first.Connection,"SELECT 1/0"));
                Assert.Equal("22012",error.SqlState);
            }
            else if(exit=="cancellation")
            {
                using var cancelled=new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await using var command=new NpgsqlCommand("SELECT pg_sleep(30)",first.Connection,first.Transaction);
                var pending=command.ExecuteNonQueryAsync(cancelled.Token);
                try
                {
                    await using var observer=await fixture.Reader.OpenConnectionAsync();
                    var deadline=DateTime.UtcNow.AddSeconds(5);
                    while(!await Scalar<bool>(observer,"SELECT COALESCE((SELECT wait_event='PgSleep' FROM pg_stat_activity WHERE pid=$1),false)",pid))
                    {
                        if(DateTime.UtcNow>deadline) throw new TimeoutException("Cancellation target did not enter its query.");
                        await Task.Delay(10);
                    }
                    cancelled.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>{await pending;});
                }
                finally
                {
                    cancelled.Cancel();
                    try { await pending; } catch(OperationCanceledException) { }
                }
            }
        }
        // Explicitly reused physical connector, not just a new logical connection.
        await using(var second=await Scope(fixture.Alice,fixture.B,source:pool))
        {
            Assert.Equal(pid,second.Connection.ProcessID);
            Assert.Equal(fixture.B,await Scalar<Guid>(second.Connection,"SELECT organization_id FROM organizations.branches"));
            Assert.Equal(0L,await Scalar<long>(second.Connection,"SELECT count(*) FROM organizations.branches WHERE organization_id=$1",fixture.A));
        }
        await using var unset=await pool.OpenConnectionAsync();
        Assert.Equal(pid,unset.ProcessID);
        await EmptyProtected(unset);
    }
    [Fact]
    public async Task CancellationBeforeOpenAndEmptyScopeAreRejected()
    {
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ReadOnlyTenantSession.OpenAsync(fixture.Reader,
            TrustedSyntheticActor.FromTrustedHarness(fixture.Alice),fixture.A,fixture.Branch,cancelled.Token));
        await Assert.ThrowsAsync<ArgumentException>(()=>ReadOnlyTenantSession.OpenAsync(fixture.Reader,
            TrustedSyntheticActor.FromTrustedHarness(fixture.Alice),Guid.Empty,fixture.Branch));
    }
    [Fact]
    public async Task ParallelIndependentScopesDoNotShareTenantContext()
    {
        async Task<Guid> Read(Guid organization)
        {
            await using var scope=await Scope(fixture.Alice,organization);
            return await Scalar<Guid>(scope.Connection,"SELECT organization_id FROM organizations.branches");
        }
        var results=await Task.WhenAll(Read(fixture.A),Read(fixture.B));
        Assert.Equal(new[]{fixture.A,fixture.B},results);
    }
    [Fact]
    public async Task CompletePrivilegeAndFunctionSurfaceIsNarrow()
    {
        await using var c=await fixture.Reader.OpenConnectionAsync();
        foreach(var table in new[]{"identity.staff_users","organizations.organizations","organizations.branches","organizations.memberships","organizations.membership_branch_grants","organizations.membership_capabilities","organizations.capabilities"})
        {
            Assert.True(await Scalar<bool>(c,"SELECT has_table_privilege(current_user,$1,'SELECT')",table));
            foreach(var privilege in new[]{"INSERT","UPDATE","DELETE","TRUNCATE","TRIGGER","REFERENCES"})
                Assert.False(await Scalar<bool>(c,"SELECT has_table_privilege(current_user,$1,$2)",table,privilege));
            Assert.False(await Scalar<bool>(c,"SELECT has_any_column_privilege(current_user,$1,'UPDATE')",table));
        }
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM pg_auth_members WHERE member='qr_reader'::regrole"));
        Assert.False(await Scalar<bool>(c,"SELECT has_database_privilege(current_user,current_database(),'TEMP')"));
        foreach(var schema in new[]{"foundation","identity","organizations","public"})
            Assert.False(await Scalar<bool>(c,"SELECT has_schema_privilege(current_user,$1,'CREATE')",schema));
        Assert.Equal(1L,await Scalar<long>(c,"SELECT count(*) FROM pg_proc WHERE pronamespace IN ('foundation'::regnamespace,'organizations'::regnamespace) AND prosecdef"));
        Assert.True(await Scalar<bool>(c,"SELECT bool_and(proconfig=ARRAY['search_path=pg_catalog']) FROM pg_proc WHERE pronamespace IN ('foundation'::regnamespace,'organizations'::regnamespace)"));
        Assert.Equal(8L,await Scalar<long>(c,"SELECT count(*) FROM pg_policies WHERE schemaname IN ('identity','organizations')"));
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM pg_policies WHERE cmd<>'SELECT' AND roles<>ARRAY['qr_revision_maintainer']::name[]"));
        Assert.False(await Scalar<bool>(c,"SELECT has_column_privilege('qr_revision_maintainer','organizations.memberships','status','UPDATE')"));
        Assert.True(await Scalar<bool>(c,"SELECT has_column_privilege('qr_revision_maintainer','organizations.memberships','authorization_revision','UPDATE')"));
        Assert.Equal(0L,await Scalar<long>(c,"SELECT count(*) FROM pg_auth_members WHERE roleid='qr_revision_maintainer'::regrole"));
    }
    [Fact]
    public async Task TransactionBoundaryRejectsAdministrativeDatasourceWiring()
    {
        await using var admin=NpgsqlDataSource.Create(LocalDatabase.ConnectionString());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>ReadOnlyTenantSession.OpenAsync(admin,
            TrustedSyntheticActor.FromTrustedHarness(fixture.Alice),fixture.A,fixture.Branch));
    }
}
