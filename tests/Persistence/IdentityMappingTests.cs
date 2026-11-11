using DatabaseSetup;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
using static Foundation.PersistenceTests.RlsFixture;

namespace Foundation.PersistenceTests;

public sealed class IdentityMappingFixture : IAsyncLifetime
{
    public Guid IssuerId { get; } = Guid.NewGuid();
    public Guid OtherIssuerId { get; } = Guid.NewGuid();
    public Guid User { get; } = Guid.NewGuid();
    public Guid OtherUser { get; } = Guid.NewGuid();
    public Guid DisabledUser { get; } = Guid.NewGuid();
    public string Issuer { get; } = "https://a-" + Guid.NewGuid().ToString("N") + ".synthetic.invalid";
    public string OtherIssuer { get; } = "https://b-" + Guid.NewGuid().ToString("N") + ".synthetic.invalid";
    public const string Subject = "Synthetic-Exact-Subject";
    public NpgsqlDataSource Admin { get; } = NpgsqlDataSource.Create(LocalDatabase.ConnectionString());

    public async Task InitializeAsync()
    {
        if (!LocalDatabase.IsCi) throw new InvalidOperationException("Mapping tests require a fresh disposable CI target.");
        await using (var check = await SyntheticIdentityMappingSession.OpenAsync(Admin)) { }
        await using var c = await Admin.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        await Exec(c, "INSERT INTO identity.staff_users VALUES ($1,true),($2,true),($3,false)", User, OtherUser, DisabledUser);
        await Exec(c, "INSERT INTO identity.authentication_issuers(issuer_id,issuer,is_enabled) VALUES ($1,$2,true),($3,$4,true)", IssuerId, Issuer, OtherIssuerId, OtherIssuer);
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,state) VALUES ($1,$2,$3,'active'),($4,$2,$5,'active')", IssuerId, Subject, User, OtherIssuerId, OtherUser);
        await tx.CommitAsync();
    }

    public async Task DisposeAsync()
    {
        await using var c = await Admin.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        // Privileged teardown only for this fixture's generated IDs, with trigger state restored atomically.
        await Exec(c, "ALTER TABLE identity.external_staff_identities DISABLE TRIGGER mapping_integrity");
        await Exec(c, "DELETE FROM identity.external_staff_identities WHERE issuer_id=ANY($1)", new[] { IssuerId, OtherIssuerId });
        await Exec(c, "ALTER TABLE identity.external_staff_identities ENABLE TRIGGER mapping_integrity");
        await Exec(c, "ALTER TABLE identity.authentication_issuers DISABLE TRIGGER issuer_integrity");
        await Exec(c, "DELETE FROM identity.authentication_issuers WHERE issuer_id=ANY($1)", new[] { IssuerId, OtherIssuerId });
        await Exec(c, "ALTER TABLE identity.authentication_issuers ENABLE TRIGGER issuer_integrity");
        await Exec(c, "DELETE FROM identity.staff_users WHERE user_id=ANY($1)", new[] { User, OtherUser, DisabledUser });
        await tx.CommitAsync();
        await Admin.DisposeAsync();
    }
}

public sealed class IdentityMappingTests(IdentityMappingFixture fixture) : IClassFixture<IdentityMappingFixture>
{
    private static ValidatedAuthentication Evidence(string issuer, string subject = IdentityMappingFixture.Subject)
        => new(new ExternalPrincipalKey(issuer, subject));
    private PostgreSqlExternalStaffIdentityResolver Resolver(SyntheticIdentityMappingSession session)
        => new(session, new[] { fixture.Issuer, fixture.OtherIssuer });
    private sealed class SyntheticSource(ValidatedAuthentication? evidence) : IValidatedAuthenticationSource
    {
        public ValueTask<ValidatedAuthentication?> ReadCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(evidence);
        }
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("unknown")]
    [InlineData("unvalidated")]
    public async Task ExistingActorBoundaryComposesOnlyWithValidatedExactMapping(string scenario)
    {
        await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        var source = new SyntheticSource(scenario == "unvalidated" ? null : Evidence(fixture.Issuer,
            scenario == "unknown" ? Guid.NewGuid().ToString() : IdentityMappingFixture.Subject));
        var boundary = new AuthenticatedActorBoundary(source, Resolver(session));
        var actor = await boundary.ResolveCurrentAsync();
        if (scenario == "valid") Assert.Equal(fixture.User, actor!.UserId);
        else Assert.Null(actor);
    }

    [Fact]
    public async Task ConcurrentIndependentSnapshotsCannotExchangeActors()
    {
        async Task<Guid> Resolve(string issuer)
        {
            await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
            var actor = await new AuthenticatedActorBoundary(new SyntheticSource(Evidence(issuer)), Resolver(session)).ResolveCurrentAsync();
            return actor!.UserId;
        }
        var actors = await Task.WhenAll(Resolve(fixture.Issuer), Resolve(fixture.OtherIssuer));
        Assert.Equal(new[] { fixture.User, fixture.OtherUser }, actors);
    }

    private async Task InTransaction(Func<NpgsqlConnection, Task> run)
    {
        await using var c = await fixture.Admin.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        try { await run(c); } finally { await tx.RollbackAsync(); }
    }
    private static async Task Reject(NpgsqlConnection c, string sql, string state, params object[] values)
    {
        await Exec(c, "SAVEPOINT rejected");
        var error = await Assert.ThrowsAsync<PostgresException>(() => Exec(c, sql, values));
        Assert.Equal(state, error.SqlState);
        await Exec(c, "ROLLBACK TO SAVEPOINT rejected");
    }

    [Theory]
    [InlineData("first")]
    [InlineData("second")]
    [InlineData("unknown-subject")]
    [InlineData("subject-case")]
    [InlineData("issuer-case")]
    [InlineData("issuer-slash")]
    [InlineData("forged-issuer")]
    [InlineData("sql-subject")]
    public async Task ExactResolverDoesNotAliasOrSubstituteIdentities(string scenario)
    {
        await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        var issuer = scenario == "second" ? fixture.OtherIssuer : scenario == "issuer-case" ? fixture.Issuer.ToUpperInvariant()
            : scenario == "issuer-slash" ? fixture.Issuer + "/" : scenario == "forged-issuer" ? "https://forged.synthetic.invalid" : fixture.Issuer;
        var subject = scenario == "unknown-subject" ? Guid.NewGuid().ToString() : scenario == "subject-case" ? IdentityMappingFixture.Subject.ToLowerInvariant()
            : scenario == "sql-subject" ? "' OR true --" : IdentityMappingFixture.Subject;
        var result = await Resolver(session).ResolveAsync(Evidence(issuer, subject), default);
        Assert.Equal(scenario is "first" or "second" ? ExternalIdentityResolutionStatus.Unique : ExternalIdentityResolutionStatus.Missing, result!.Status);
        Assert.Equal(new ExternalPrincipalKey(issuer, subject), result.Principal);
        if (scenario is "first" or "second") Assert.Equal(scenario == "first" ? fixture.User : fixture.OtherUser, result.Staff!.UserId);
        else Assert.Null(result.Staff);
    }

    [Fact]
    public async Task RegistryEnablementCannotReplaceTrustedIssuerAllowlist()
    {
        await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        var config = new List<string> { fixture.OtherIssuer };
        var resolver = new PostgreSqlExternalStaffIdentityResolver(session, config);
        config.Add(fixture.Issuer);
        Assert.Equal(ExternalIdentityResolutionStatus.Missing, (await resolver.ResolveAsync(Evidence(fixture.Issuer), default))!.Status);
        Assert.Equal(ExternalIdentityResolutionStatus.Unique, (await resolver.ResolveAsync(Evidence(fixture.OtherIssuer), default))!.Status);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("unlinked")]
    [InlineData("disabled-staff")]
    [InlineData("disabled-issuer")]
    public async Task InactiveMappingsNeverResolveEnabledAuthority(string scenario)
    {
        var subject = Guid.NewGuid().ToString();
        await using var c = await fixture.Admin.OpenConnectionAsync();
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,state) VALUES ($1,$2,$3,$4)",
            fixture.IssuerId, subject, scenario == "disabled-staff" ? fixture.DisabledUser : fixture.User,
            scenario is "disabled" or "unlinked" ? scenario : "active");
        if (scenario == "disabled-issuer") await Exec(c, "UPDATE identity.authentication_issuers SET is_enabled=false WHERE issuer_id=$1", fixture.IssuerId);
        try
        {
            await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
            var result = await Resolver(session).ResolveAsync(Evidence(fixture.Issuer, subject), default);
            Assert.Equal(ExternalIdentityResolutionStatus.Missing, result!.Status);
            Assert.Null(result.Staff);
        }
        finally
        {
            if (scenario == "disabled-issuer") await Exec(c, "UPDATE identity.authentication_issuers SET is_enabled=true WHERE issuer_id=$1", fixture.IssuerId);
        }
    }

    [Fact]
    public async Task TwoExactIdentitiesMayResolveOneUserWithoutEmailLinking()
    {
        var subject = Guid.NewGuid().ToString();
        await using var c = await fixture.Admin.OpenConnectionAsync();
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,state) VALUES ($1,$2,$3,'active')", fixture.OtherIssuerId, subject, fixture.User);
        await using var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        Assert.Equal(fixture.User, (await Resolver(session).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff!.UserId);
        Assert.Equal(fixture.User, (await Resolver(session).ResolveAsync(Evidence(fixture.OtherIssuer, subject), default))!.Staff!.UserId);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("disabled")]
    [InlineData("unlinked")]
    public Task DuplicateTupleCannotCreateAmbiguousOwnershipInAnyState(string state) => InTransaction(async c =>
    {
        var subject = Guid.NewGuid().ToString();
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,state) VALUES ($1,$2,$3,$4)", fixture.IssuerId, subject, fixture.User, state);
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", "23505", fixture.IssuerId, subject, fixture.OtherUser);
        Assert.Equal(1L, await Scalar<long>(c, "SELECT count(*) FROM identity.external_staff_identities WHERE issuer_id=$1 AND subject=$2", fixture.IssuerId, subject));
        await Reject(c, "INSERT INTO identity.authentication_issuers(issuer) VALUES ($1)", "23505", fixture.Issuer);
    });

    [Theory]
    [InlineData("")]
    [InlineData("http://issuer.invalid")]
    [InlineData("https://user@issuer.invalid")]
    [InlineData("https://issuer.invalid?query=x")]
    [InlineData("https://issuer.invalid#fragment")]
    [InlineData(" https://issuer.invalid")]
    [InlineData("https://issuer.invalid/\npath")]
    [InlineData("https://issuer.invalid/\u00a0path")]
    public Task InvalidIssuerStorageIsRejected(string issuer) => InTransaction(c =>
        Reject(c, "INSERT INTO identity.authentication_issuers(issuer) VALUES ($1)", "23514", issuer));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\u00a0\u2000\u3000")]
    [InlineData("sub\nject")]
    [InlineData("sub\u0085ject")]
    [InlineData("sub\u007fject")]
    public Task InvalidSubjectStorageIsRejected(string subject) => InTransaction(c =>
        Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", "23514", fixture.IssuerId, subject, fixture.User));

    [Fact]
    public Task ByteBoundsAndExactUnicodeKeysAreEnforced() => InTransaction(async c =>
    {
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", fixture.IssuerId, new string('é', 512), fixture.User);
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", "23514", fixture.IssuerId, new string('é', 513), fixture.User);
        await Reject(c, "INSERT INTO identity.authentication_issuers(issuer) VALUES ($1)", "23514", "https://" + new string('x', 1017));
        await Exec(c, "INSERT INTO identity.authentication_issuers(issuer) VALUES ($1)", "https://" + new string('x', 1016));
        foreach (var subject in new[] { "é", "e\u0301", "Case", "case", " padded " })
            await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", fixture.IssuerId, subject, fixture.User);
        Assert.Equal(5L, await Scalar<long>(c, "SELECT count(*) FROM identity.external_staff_identities WHERE issuer_id=$1 AND subject=ANY($2)", fixture.IssuerId, new[] { "é", "e\u0301", "Case", "case", " padded " }));
    });

    [Theory]
    [InlineData("mapping_id=gen_random_uuid()")]
    [InlineData("issuer_id=gen_random_uuid()")]
    [InlineData("user_id=gen_random_uuid()")]
    [InlineData("subject='replacement'")]
    [InlineData("revision=0")]
    [InlineData("revision=99")]
    [InlineData("created_at=created_at-interval '1 day'")]
    [InlineData("updated_at=updated_at+interval '1 day'")]
    public Task MappingKeysOwnerAndDatabaseMetadataAreImmutable(string mutation) => InTransaction(c =>
        Reject(c, $"UPDATE identity.external_staff_identities SET {mutation} WHERE issuer_id=$1", "23514", fixture.IssuerId));

    [Theory]
    [InlineData("issuer_id=gen_random_uuid()")]
    [InlineData("issuer='https://replacement.invalid'")]
    [InlineData("revision=99")]
    public Task IssuerKeysAndRevisionAreImmutable(string mutation) => InTransaction(c =>
        Reject(c, $"UPDATE identity.authentication_issuers SET {mutation} WHERE issuer_id=$1", "23514", fixture.IssuerId));

    [Theory]
    [InlineData("DELETE FROM identity.external_staff_identities")]
    [InlineData("DELETE FROM identity.authentication_issuers")]
    [InlineData("TRUNCATE identity.external_staff_identities")]
    [InlineData("TRUNCATE identity.authentication_issuers CASCADE")]
    public Task TombstonesCannotBeDeletedOrTruncated(string sql) => InTransaction(c => Reject(c, sql, "23514"));

    [Fact]
    public Task LifecycleRevisionsAndRollbackAreAtomic() => InTransaction(async c =>
    {
        var subject = Guid.NewGuid().ToString();
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,created_at,updated_at) VALUES ($1,$2,$3,'2000-01-01','2000-01-01')", fixture.IssuerId, subject, fixture.User);
        Assert.Equal("disabled", await Scalar<string>(c, "SELECT state FROM identity.external_staff_identities WHERE subject=$1", subject));
        Assert.True(await Scalar<bool>(c, "SELECT created_at=updated_at AND created_at>'2026-01-01' FROM identity.external_staff_identities WHERE subject=$1", subject));
        await Exec(c, "SAVEPOINT lifecycle");
        foreach (var state in new[] { "active", "disabled", "unlinked", "unlinked" })
            await Exec(c, "UPDATE identity.external_staff_identities SET state=$1 WHERE subject=$2", state, subject);
        Assert.Equal(5L, await Scalar<long>(c, "SELECT revision FROM identity.external_staff_identities WHERE subject=$1", subject));
        await Reject(c, "UPDATE identity.external_staff_identities SET state='active' WHERE subject=$1", "23514", subject);
        await Exec(c, "ROLLBACK TO SAVEPOINT lifecycle");
        Assert.Equal(1L, await Scalar<long>(c, "SELECT revision FROM identity.external_staff_identities WHERE subject=$1", subject));
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id,revision) VALUES ($1,'bad-revision',$2,2)", "23514", fixture.IssuerId, fixture.User);
        await Reject(c, "UPDATE identity.external_staff_identities SET state='unknown' WHERE subject=$1", "23514", subject);
    });

    [Theory]
    [InlineData("authentication_issuers", "issuer_integrity")]
    [InlineData("external_staff_identities", "mapping_integrity")]
    public Task RevisionOverflowFailsWithoutMutation(string table, string trigger) => InTransaction(async c =>
    {
        await Exec(c, $"ALTER TABLE identity.{table} DISABLE TRIGGER {trigger}");
        await Exec(c, $"UPDATE identity.{table} SET revision=9223372036854775807 WHERE issuer_id=$1", fixture.IssuerId);
        await Exec(c, $"ALTER TABLE identity.{table} ENABLE TRIGGER {trigger}");
        await Reject(c, $"UPDATE identity.{table} SET revision=revision WHERE issuer_id=$1", "22003", fixture.IssuerId);
        Assert.Equal(long.MaxValue, await Scalar<long>(c, $"SELECT revision FROM identity.{table} WHERE issuer_id=$1 LIMIT 1", fixture.IssuerId));
    });

    [Fact]
    public Task ForeignKeysRejectMissingOwnersAndProtectExistingStaff() => InTransaction(async c =>
    {
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,'missing-staff',$2)", "23503", fixture.IssuerId, Guid.NewGuid());
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,'missing-issuer',$2)", "23503", Guid.NewGuid(), fixture.User);
        await Reject(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,'zero',$2)", "23514", fixture.IssuerId, Guid.Empty);
        await Reject(c, "DELETE FROM identity.staff_users WHERE user_id=$1", "23001", fixture.User);
        Assert.Equal(2L, await Scalar<long>(c, "SELECT count(*) FROM pg_constraint WHERE conrelid='identity.external_staff_identities'::regclass AND contype='f' AND confdeltype='r' AND confupdtype='r'"));
    });

    [Theory]
    [InlineData("qr_reader")]
    [InlineData("qr_setup")]
    [InlineData("qr_migrator")]
    public async Task ExistingRolesCannotEnterPrivilegedResolverOrReadMappingTables(string role)
    {
        await using var pool = NpgsqlDataSource.Create(LocalDatabase.RoleConnectionString(role));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SyntheticIdentityMappingSession.OpenAsync(pool));
        await using var c = await pool.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        await Exec(c, "SET TRANSACTION READ WRITE");
        foreach (var table in new[] { "authentication_issuers", "external_staff_identities" })
        {
            await Reject(c, $"SELECT * FROM identity.{table}", "42501");
            await using var admin = await fixture.Admin.OpenConnectionAsync();
            Assert.False(await Scalar<bool>(admin, "SELECT has_table_privilege($1,$2,'SELECT')", role, "identity." + table));
        }
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task OwnerRemainsForcedAndNoNewPoliciesOrDefinerFunctionsExist()
    {
        await using var c = await fixture.Admin.OpenConnectionAsync();
        Assert.Equal(0L, await Scalar<long>(c, "SELECT count(*) FROM pg_policies WHERE schemaname='identity' AND tablename IN ('authentication_issuers','external_staff_identities')"));
        Assert.True(await Scalar<bool>(c, "SELECT bool_and(NOT prosecdef AND proconfig=ARRAY['search_path=pg_catalog'] AND proowner='qr_platform_owner'::regrole) FROM pg_proc WHERE pronamespace='identity'::regnamespace"));
        await using var tx = await c.BeginTransactionAsync();
        await Exec(c, "SET LOCAL ROLE qr_platform_owner");
        foreach (var table in new[] { "authentication_issuers", "external_staff_identities" })
            Assert.Equal(0L, await Scalar<long>(c, $"SELECT count(*) FROM identity.{table}"));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task CommittedDisablementIsVisibleOnlyToNewSnapshots()
    {
        await using var before = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        Assert.NotNull((await Resolver(before).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff);
        await using var c = await fixture.Admin.OpenConnectionAsync();
        await Exec(c, "UPDATE identity.authentication_issuers SET is_enabled=false WHERE issuer_id=$1", fixture.IssuerId);
        try
        {
            Assert.NotNull((await Resolver(before).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff);
            await using var after = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
            Assert.Null((await Resolver(after).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff);
        }
        finally { await Exec(c, "UPDATE identity.authentication_issuers SET is_enabled=true WHERE issuer_id=$1", fixture.IssuerId); }
    }

    [Fact]
    public async Task ConcurrentDuplicateInsertCannotCreateSecondOwner()
    {
        var subject = Guid.NewGuid().ToString();
        await using var first = await fixture.Admin.OpenConnectionAsync();
        await using var second = await fixture.Admin.OpenConnectionAsync();
        await using var firstTx = await first.BeginTransactionAsync();
        await using var secondTx = await second.BeginTransactionAsync();
        await Exec(first, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", fixture.IssuerId, subject, fixture.User);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var command = new NpgsqlCommand("INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", second, secondTx);
        command.Parameters.AddWithValue(fixture.IssuerId); command.Parameters.AddWithValue(subject); command.Parameters.AddWithValue(fixture.OtherUser);
        var pending = command.ExecuteNonQueryAsync(deadline.Token);
        var committed = false;
        try
        {
            await using var observer = await fixture.Admin.OpenConnectionAsync();
            while (!await Scalar<bool>(observer, "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid=$1 AND wait_event_type='Lock')", second.ProcessID))
                await Task.Delay(10, deadline.Token);
            await firstTx.CommitAsync(deadline.Token);
            committed = true;
            var error = await Assert.ThrowsAsync<PostgresException>(async () => await pending);
            Assert.Equal("23505", error.SqlState);
            Assert.Equal(1L, await Scalar<long>(observer, "SELECT count(*) FROM identity.external_staff_identities WHERE issuer_id=$1 AND subject=$2", fixture.IssuerId, subject));
        }
        finally
        {
            deadline.Cancel();
            if (!committed) await firstTx.RollbackAsync();
            await secondTx.RollbackAsync();
            try { await pending; } catch (Exception error) when (error is PostgresException or OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task EfRefreshesDatabaseRevisionAndRejectsStaleWriter()
    {
        await using var c = await fixture.Admin.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var subject = Guid.NewGuid().ToString();
        await Exec(c, "INSERT INTO identity.external_staff_identities(issuer_id,subject,user_id) VALUES ($1,$2,$3)", fixture.IssuerId, subject, fixture.User);
        await using var context = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(c).Options);
        await context.Database.UseTransactionAsync(tx);
        var entries = context.Set<Dictionary<string, object>>("ExternalStaffIdentity");
        var entry = await entries.SingleAsync(e => EF.Property<string>(e, "subject") == subject);
        entry["state"] = "active";
        await context.SaveChangesAsync();
        Assert.Equal(2L, entry["revision"]);
        entry["state"] = "disabled";
        await context.SaveChangesAsync();
        Assert.Equal(3L, entry["revision"]);
        await Exec(c, "UPDATE identity.external_staff_identities SET state='active' WHERE subject=$1", subject);
        entry["state"] = "unlinked";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
        Assert.Equal(4L, await Scalar<long>(c, "SELECT revision FROM identity.external_staff_identities WHERE subject=$1", subject));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task CancelledCallsAndDisposedSessionsCannotYieldIdentity()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin, cancelled.Token));
        var session = await SyntheticIdentityMappingSession.OpenAsync(fixture.Admin);
        var resolver = Resolver(session);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await resolver.ResolveAsync(Evidence(fixture.Issuer), cancelled.Token));
        Assert.Equal(ExternalIdentityResolutionStatus.Missing, (await resolver.ResolveAsync(Evidence(fixture.Issuer, new string('x', 1025)), default))!.Status);
        await session.DisposeAsync();
        await session.DisposeAsync();
        Assert.Equal(ExternalIdentityResolutionStatus.Missing, (await resolver.ResolveAsync(Evidence(fixture.Issuer), default))!.Status);
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("error")]
    [InlineData("cancellation")]
    public async Task RollbackAndPhysicalReuseLeaveNoTransactionOrIdentityState(string outcome)
    {
        var settings = new NpgsqlConnectionStringBuilder(LocalDatabase.ConnectionString()) { MaxPoolSize = 1, MinPoolSize = 1 };
        await using var pool = NpgsqlDataSource.Create(settings.ConnectionString);
        int pid;
        await using (var session = await SyntheticIdentityMappingSession.OpenAsync(pool))
        {
            pid = session.Connection.ProcessID;
            Assert.Equal("repeatable read", await Scalar<string>(session.Connection, "SHOW transaction_isolation"));
            Assert.Equal("on", await Scalar<string>(session.Connection, "SHOW transaction_read_only"));
            await Exec(session.Connection, "SELECT set_config('qr.synthetic_marker','one',true)");
            if (outcome == "normal") Assert.NotNull((await Resolver(session).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff);
            if (outcome == "error")
                Assert.Equal("25006", (await Assert.ThrowsAsync<PostgresException>(() => Exec(session.Connection, "UPDATE identity.authentication_issuers SET is_enabled=false"))).SqlState);
            if (outcome == "cancellation")
            {
                await using var blocker = await fixture.Admin.OpenConnectionAsync();
                await using var tx = await blocker.BeginTransactionAsync();
                await Exec(blocker, "LOCK TABLE identity.external_staff_identities IN ACCESS EXCLUSIVE MODE");
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var pending = Resolver(session).ResolveAsync(Evidence(fixture.Issuer), deadline.Token).AsTask();
                while (!await Scalar<bool>(blocker, "SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid=$1 AND wait_event_type='Lock')", pid))
                    await Task.Delay(10, deadline.Token);
                deadline.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
                await tx.RollbackAsync();
            }
        }
        await using var reused = await pool.OpenConnectionAsync();
        Assert.Equal(pid, reused.ProcessID);
        Assert.Equal("read committed", await Scalar<string>(reused, "SHOW transaction_isolation"));
        Assert.Equal("off", await Scalar<string>(reused, "SHOW transaction_read_only"));
        Assert.True(string.IsNullOrEmpty(await Scalar<string>(reused, "SELECT coalesce(current_setting('qr.synthetic_marker',true),'')")));
        await reused.CloseAsync();
        await using var next = await SyntheticIdentityMappingSession.OpenAsync(pool);
        Assert.Equal(fixture.User, (await Resolver(next).ResolveAsync(Evidence(fixture.Issuer), default))!.Staff!.UserId);
    }

    [Fact]
    public async Task ReviewSqlMatchesBothPreservedRlsAndNewForwardMigration()
    {
        await using var context = LocalDatabase.CreateContext(LocalDatabase.ConnectionString());
        var migrator = context.GetService<IMigrator>();
        static string Normalize(string sql) => sql.Replace("\r\n", "\n").Trim();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "global.json"))) root = root.Parent;
        Assert.NotNull(root);
        Assert.Equal(Normalize(await File.ReadAllTextAsync(Path.Combine(root.FullName, "infrastructure/development/tenant-row-security.sql"))),
            Normalize(migrator.GenerateScript("20261026033754_InitialFoundation", "20261026040035_TenantRowSecurity")));
        Assert.Equal(Normalize(await File.ReadAllTextAsync(Path.Combine(root.FullName, "infrastructure/development/identity-mappings.sql"))),
            Normalize(migrator.GenerateScript("20261026040035_TenantRowSecurity", "20261111233123_IdentityMappings")));
        Assert.Throws<NotSupportedException>(() => migrator.GenerateScript("20261111233123_IdentityMappings", "20261026040035_TenantRowSecurity"));
    }
}
