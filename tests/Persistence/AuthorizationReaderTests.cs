using DatabaseSetup;
using Identity.Infrastructure;
using Npgsql;
using Organizations.Application;
using Organizations.Domain;
using Organizations.Infrastructure;
using PostgreSql.Infrastructure;
using Xunit;
using static Foundation.PersistenceTests.RlsFixture;

namespace Foundation.PersistenceTests;

// Real PostgreSQL, trusted synthetic actor only. No API registration or login is exercised.
public sealed class AuthorizationReaderTests(RlsFixture fixture) : IClassFixture<RlsFixture>
{
    private Task<ReadOnlyTenantSession> Open(Guid? actor = null, Guid? organization = null,
        Guid? branch = null, NpgsqlDataSource? pool = null)
        => ReadOnlyTenantSession.OpenAsync(pool ?? fixture.Reader,
            TrustedSyntheticActor.FromTrustedHarness(actor ?? fixture.Alice),
            organization ?? fixture.A, branch ?? fixture.Branch);

    private static StaffAuthorization Compose(ReadOnlyTenantSession session)
        => new(new PostgreSqlStaffIdentityReader(session), new PostgreSqlOrganizationAccessReader(session));

    private static ValueTask<OrganizationAccessSnapshot?> Read(ReadOnlyTenantSession session)
        => new PostgreSqlOrganizationAccessReader(session).ReadCurrentAsync(
            session.ActorId, session.OrganizationId, session.BranchId, CancellationToken.None);

    private static AccessRequest Request(ReadOnlyTenantSession session, long revision,
        Capability capability = Capability.WorkspaceRead)
        => new(session.ActorId, session.OrganizationId, session.BranchId, revision, capability);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MapsImmutableSnapshotAndAuthorizesIndependentMemberships(bool tenantB)
    {
        var organization = tenantB ? fixture.B : fixture.A;
        await using var setup = new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_setup"));
        await setup.OpenAsync();
        var expectedRevision = await Scalar<long>(setup,
            "SELECT authorization_revision FROM organizations.memberships WHERE organization_id=$1 AND user_id=$2",
            organization, fixture.Alice);
        OrganizationAccessSnapshot snapshot;
        await using (var session = await Open(organization: organization))
        {
            Assert.Equal("repeatable read", await Scalar<string>(session.Connection, "SHOW transaction_isolation"));
            Assert.Equal("on", await Scalar<string>(session.Connection, "SHOW transaction_read_only"));
            var identity = await new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(fixture.Alice, default);
            Assert.NotNull(identity);
            Assert.Equal(fixture.Alice, identity.UserId);
            Assert.True(identity.IsEnabled);
            snapshot = Assert.IsType<OrganizationAccessSnapshot>(await Read(session));
            Assert.Equal(new PharmacyOrganization(organization, OrganizationStatus.Active), snapshot.Organization);
            Assert.Equal(new Branch(fixture.Branch, organization, true), snapshot.Branch);
            var membership = Assert.IsType<Membership>(snapshot.Membership);
            Assert.Equal(fixture.Alice, membership.UserId);
            Assert.Equal(organization, membership.OrganizationId);
            Assert.Equal(MembershipStatus.Active, membership.Status);
            Assert.Equal(expectedRevision, membership.Revision);
            Assert.True(membership.BranchIds.SetEquals(tenantB
                ? new[] { fixture.Branch } : new[] { fixture.Branch, fixture.DisabledBranch }));
            Assert.True(membership.Capabilities.SetEquals(new[] { Capability.WorkspaceRead }));
            Assert.Empty(membership.DelegableCapabilities);
            var context = await Compose(session).AuthorizeAsync(Request(session, membership.Revision));
            Assert.NotNull(context);
            Assert.Equal(organization, context.OrganizationId);
            Assert.Equal(fixture.Branch, context.BranchId);
            Assert.Equal(membership.Revision, context.Revision);
            await session.CommitAsync();
            Assert.Null(await Read(session));
        }
        // Fully materialized immutable data survives the transaction; it is not reusable authority.
        Assert.Contains(Capability.WorkspaceRead, snapshot.Membership!.Capabilities);
        var branchSet = Assert.IsAssignableFrom<ISet<Guid>>(snapshot.Membership.BranchIds);
        Assert.True(branchSet.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => branchSet.Add(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("organization")]
    [InlineData("branch")]
    [InlineData("empty-actor")]
    [InlineData("empty-organization")]
    [InlineData("empty-branch")]
    public async Task ReaderArgumentsCannotSubstituteOriginalHarnessScope(string substitution)
    {
        await using var session = await Open();
        var revision = (await Read(session))!.Membership!.Revision;
        var actor = substitution == "actor" ? fixture.Bob : substitution == "empty-actor" ? Guid.Empty : fixture.Alice;
        var organization = substitution == "organization" ? fixture.B : substitution == "empty-organization" ? Guid.Empty : fixture.A;
        var branch = substitution == "branch" ? fixture.DisabledBranch : substitution == "empty-branch" ? Guid.Empty : fixture.Branch;
        Assert.Null(await new PostgreSqlOrganizationAccessReader(session).ReadCurrentAsync(actor, organization, branch, default));
        if (actor != fixture.Alice)
            Assert.Null(await new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(actor, default));
        Assert.Null(await Compose(session).AuthorizeAsync(new(actor, organization, branch, revision, Capability.WorkspaceRead)));
        Assert.NotNull(await Compose(session).AuthorizeAsync(Request(session, revision)));
    }

    [Theory]
    [InlineData("app.actor_id", "empty")]
    [InlineData("app.organization_id", "empty")]
    [InlineData("app.branch_id", "empty")]
    [InlineData("app.actor_id", "malformed")]
    [InlineData("app.organization_id", "malformed")]
    [InlineData("app.branch_id", "malformed")]
    [InlineData("app.actor_id", "zero")]
    [InlineData("app.organization_id", "zero")]
    [InlineData("app.branch_id", "zero")]
    [InlineData("app.actor_id", "other")]
    [InlineData("app.organization_id", "other")]
    [InlineData("app.branch_id", "other")]
    public async Task MissingMalformedOrChangedDatabaseContextDeniesReaders(string setting, string kind)
    {
        await using var session = await Open();
        var revision = (await Read(session))!.Membership!.Revision;
        var value = kind switch
        {
            "empty" => "",
            "malformed" => "' OR true --",
            "zero" => Guid.Empty.ToString(),
            _ => (setting == "app.actor_id" ? fixture.Bob : setting == "app.organization_id" ? fixture.B : fixture.DisabledBranch).ToString()
        };
        await Exec(session.Connection, "SELECT set_config($1,$2,true)", setting, value);
        Assert.Null(await new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(fixture.Alice, default));
        Assert.Null(await Read(session));
        Assert.Null(await Compose(session).AuthorizeAsync(Request(session, revision)));
    }

    [Theory]
    [InlineData("missing-identity")]
    [InlineData("disabled-identity")]
    [InlineData("missing-membership")]
    [InlineData("suspended-membership")]
    [InlineData("removed-membership")]
    [InlineData("unassigned-branch")]
    [InlineData("disabled-branch")]
    public async Task MissingInactiveAndWrongBranchStateCannotAuthorize(string scenario)
    {
        var actor = scenario switch
        {
            "missing-identity" => Guid.NewGuid(),
            "disabled-identity" => fixture.Disabled,
            "missing-membership" => fixture.AOnly,
            "suspended-membership" => fixture.Inactive,
            "removed-membership" => fixture.Removed,
            _ => fixture.Alice
        };
        var organization = scenario == "missing-membership" ? fixture.B : fixture.A;
        var branch = scenario == "unassigned-branch" ? fixture.Unassigned
            : scenario == "disabled-branch" ? fixture.DisabledBranch : fixture.Branch;
        await using var session = await Open(actor, organization, branch);
        var identity = await new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(actor, default);
        if (scenario == "missing-identity") Assert.Null(identity);
        if (scenario == "disabled-identity") Assert.False(identity!.IsEnabled);
        var snapshot = await Read(session);
        if (scenario is "missing-identity" or "missing-membership") Assert.Null(snapshot);
        if (scenario is "suspended-membership" or "removed-membership")
        {
            Assert.Equal(scenario == "suspended-membership" ? MembershipStatus.Suspended : MembershipStatus.Removed,
                snapshot!.Membership!.Status);
            Assert.Null(snapshot.Organization);
            Assert.Empty(snapshot.Membership.Capabilities);
        }
        Assert.Null(await Compose(session).AuthorizeAsync(Request(session, snapshot?.Membership?.Revision ?? 1)));
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("closed")]
    public async Task OrganizationLifecycleIsMappedForDenial(string status)
    {
        await fixture.Setup("UPDATE organizations.organizations SET status=$1 WHERE organization_id=$2", status, fixture.A);
        try
        {
            await using var session = await Open();
            var snapshot = Assert.IsType<OrganizationAccessSnapshot>(await Read(session));
            Assert.Equal(status == "suspended" ? OrganizationStatus.Suspended : OrganizationStatus.Closed,
                snapshot.Organization!.Status);
            Assert.Null(snapshot.Branch);
            Assert.Empty(snapshot.Membership!.BranchIds);
            Assert.Null(await Compose(session).AuthorizeAsync(Request(session, snapshot.Membership.Revision)));
        }
        finally { await fixture.Setup("UPDATE organizations.organizations SET status='active' WHERE organization_id=$1", fixture.A); }
    }

    [Theory]
    [InlineData("branch")]
    [InlineData("capability")]
    [InlineData("membership-revision")]
    public async Task RevocationAndRevisionChangesDenyInNewTransaction(string change)
    {
        long revision;
        await using (var before = await Open()) revision = (await Read(before))!.Membership!.Revision;
        if (change == "branch")
            await fixture.Setup("DELETE FROM organizations.membership_branch_grants WHERE organization_id=$1 AND user_id=$2 AND branch_id=$3", fixture.A, fixture.Alice, fixture.Branch);
        else if (change == "capability")
            await fixture.Setup("DELETE FROM organizations.membership_capabilities WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice);
        else
            await fixture.Setup("UPDATE organizations.memberships SET status=status WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice);
        try
        {
            await using var session = await Open();
            var snapshot = (await Read(session))!;
            Assert.True(snapshot.Membership!.Revision > revision);
            Assert.Null(await Compose(session).AuthorizeAsync(Request(session, revision)));
            var current = await Compose(session).AuthorizeAsync(Request(session, snapshot.Membership.Revision));
            if (change == "membership-revision") Assert.NotNull(current); else Assert.Null(current);
            // An independent tenant's grants are never unioned with this tenant's state.
            await using var other = await Open(organization: fixture.B);
            Assert.NotNull(await Compose(other).AuthorizeAsync(Request(other, (await Read(other))!.Membership!.Revision)));
        }
        finally
        {
            if (change == "branch")
                await fixture.Setup("INSERT INTO organizations.membership_branch_grants VALUES ($1,$2,$3)", fixture.A, fixture.Alice, fixture.Branch);
            else if (change == "capability")
                await fixture.Setup("INSERT INTO organizations.membership_capabilities VALUES ($1,$2,'workspace.read',false)", fixture.A, fixture.Alice);
        }
    }

    [Theory]
    [InlineData("allowed")]
    [InlineData("unknown-recipient")]
    [InlineData("self")]
    [InlineData("empty-recipient")]
    [InlineData("other-organization")]
    [InlineData("other-branch")]
    [InlineData("administration")]
    [InlineData("wrong-action")]
    [InlineData("not-delegable")]
    [InlineData("missing-evaluation-capability")]
    public async Task DelegationEvaluatesOnlyActorScope(string scenario)
    {
        await fixture.Setup("UPDATE organizations.membership_capabilities SET is_delegable=$3 WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice, scenario != "not-delegable");
        if (scenario != "missing-evaluation-capability")
            await fixture.Setup("INSERT INTO organizations.membership_capabilities VALUES ($1,$2,'delegation.scope.evaluate',false)", fixture.A, fixture.Alice);
        try
        {
            await using var session = await Open();
            var snapshot = (await Read(session))!;
            Assert.Equal(scenario == "not-delegable" ? 0 : 1, snapshot.Membership!.DelegableCapabilities.Count);
            var request = Request(session, snapshot.Membership.Revision,
                scenario == "wrong-action" ? Capability.WorkspaceRead : Capability.DelegationScopeEvaluate);
            var candidate = scenario == "self" ? fixture.Alice : scenario == "empty-recipient" ? Guid.Empty
                : scenario == "unknown-recipient" ? Guid.NewGuid() : fixture.Bob;
            var result = await Compose(session).EvaluateActorDelegationScopeAsync(request, candidate,
                scenario == "other-organization" ? fixture.B : fixture.A,
                scenario == "other-branch" ? fixture.DisabledBranch : fixture.Branch,
                scenario == "administration" ? Capability.DelegationScopeEvaluate : Capability.WorkspaceRead);
            Assert.Equal(scenario is "allowed" or "unknown-recipient", result.IsActorScopeAllowed);
        }
        finally
        {
            await fixture.Setup("DELETE FROM organizations.membership_capabilities WHERE organization_id=$1 AND user_id=$2 AND capability_code='delegation.scope.evaluate'", fixture.A, fixture.Alice);
            await fixture.Setup("UPDATE organizations.membership_capabilities SET is_delegable=false WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice);
        }
    }

    [Fact]
    public async Task BothReadersKeepOneSnapshotWhenIdentityIsDisabledBetweenReads()
    {
        await using var session = await Open();
        var identities = new PostgreSqlStaffIdentityReader(session);
        Assert.True((await identities.ReadCurrentAsync(fixture.Alice, default))!.IsEnabled);
        var snapshot = (await Read(session))!;
        await fixture.Setup("UPDATE identity.staff_users SET is_enabled=false WHERE user_id=$1", fixture.Alice);
        try
        {
            Assert.True((await identities.ReadCurrentAsync(fixture.Alice, default))!.IsEnabled);
            Assert.NotNull(await Compose(session).AuthorizeAsync(Request(session, snapshot.Membership!.Revision)));
            await using var fresh = await Open();
            Assert.False((await new PostgreSqlStaffIdentityReader(fresh).ReadCurrentAsync(fixture.Alice, default))!.IsEnabled);
            Assert.Null(await Compose(fresh).AuthorizeAsync(Request(fresh, snapshot.Membership.Revision)));
        }
        finally { await fixture.Setup("UPDATE identity.staff_users SET is_enabled=true WHERE user_id=$1", fixture.Alice); }
    }

    [Fact]
    public async Task ExistingSnapshotKeepsGrantsButFreshScopeSeesRevocation()
    {
        await using var session = await Open();
        var snapshot = (await Read(session))!;
        await fixture.Setup("UPDATE organizations.memberships SET status='suspended' WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice);
        try
        {
            Assert.NotNull(await Compose(session).AuthorizeAsync(Request(session, snapshot.Membership!.Revision)));
            await using var fresh = await Open();
            Assert.Null(await Compose(fresh).AuthorizeAsync(Request(fresh, snapshot.Membership.Revision)));
            Assert.Equal(MembershipStatus.Suspended, (await Read(fresh))!.Membership!.Status);
        }
        finally { await fixture.Setup("UPDATE organizations.memberships SET status='active' WHERE organization_id=$1 AND user_id=$2", fixture.A, fixture.Alice); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledReaderCallsNeverReturnAuthorization(bool organizations)
    {
        await using var session = await Open();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        if (organizations)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await new PostgreSqlOrganizationAccessReader(session).ReadCurrentAsync(fixture.Alice, fixture.A, fixture.Branch, cancellation.Token));
        else
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(fixture.Alice, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Compose(session).AuthorizeAsync(Request(session, 1), cancellation.Token));
    }

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("error")]
    [InlineData("cancellation")]
    public async Task ReaderCompositionDoesNotContaminateReusedPhysicalConnection(string exit)
    {
        var settings = new NpgsqlConnectionStringBuilder(LocalDatabase.RoleConnectionString("qr_reader"))
            { MaxPoolSize = 1, MinPoolSize = 1, ApplicationName = "qr-reader-reuse-" + exit };
        await using var pool = NpgsqlDataSource.Create(settings.ConnectionString);
        int pid;
        await using (var session = await Open(pool: pool))
        {
            pid = session.Connection.ProcessID;
            // Acquire no table locks in this transaction before the blocking cancellation probe.
            if (exit == "cancellation") await CancelBlockedReader(session);
            else
            {
                var snapshot = (await Read(session))!;
                Assert.NotNull(await Compose(session).AuthorizeAsync(Request(session, snapshot.Membership!.Revision)));
            }
            if (exit == "commit") await session.CommitAsync();
            if (exit == "error")
            {
                await Assert.ThrowsAsync<PostgresException>(() => Exec(session.Connection, "SELECT 1/0"));
                await Assert.ThrowsAsync<PostgresException>(async () => await Read(session));
            }
        }
        await using (var other = await Open(organization: fixture.B, pool: pool))
        {
            Assert.Equal(pid, other.Connection.ProcessID);
            var snapshot = (await Read(other))!;
            Assert.NotNull(await Compose(other).AuthorizeAsync(Request(other, snapshot.Membership!.Revision)));
            Assert.Equal(fixture.B, snapshot.Organization!.Id);
        }
        await using var unset = await pool.OpenConnectionAsync();
        Assert.Equal(pid, unset.ProcessID);
        Assert.Equal(0L, await Scalar<long>(unset, "SELECT count(*) FROM identity.staff_users"));
        Assert.Equal(0L, await Scalar<long>(unset, "SELECT count(*) FROM organizations.memberships"));
    }

    private async Task CancelBlockedReader(ReadOnlyTenantSession session)
    {
        await using var blocker = new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_setup"));
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await Exec(blocker, "LOCK TABLE identity.staff_users IN ACCESS EXCLUSIVE MODE");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var pending = new PostgreSqlStaffIdentityReader(session).ReadCurrentAsync(fixture.Alice, cancellation.Token).AsTask();
        try
        {
            await using var observer = await fixture.Reader.OpenConnectionAsync();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!await Scalar<bool>(observer,
                "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid=$1 AND wait_event_type='Lock')", session.Connection.ProcessID))
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("Reader did not enter its blocked query.");
                await Task.Delay(10);
            }
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        }
        finally
        {
            cancellation.Cancel();
            await transaction.RollbackAsync();
            try { await pending; } catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task DisposedReadersCannotRunInAReusedSession()
    {
        var session = await Open();
        var identities = new PostgreSqlStaffIdentityReader(session);
        await session.DisposeAsync();
        Assert.Null(await identities.ReadCurrentAsync(fixture.Alice, default));
        Assert.Null(await Read(session));
    }
}
