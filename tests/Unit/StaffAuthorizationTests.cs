using Identity.Application;
using Organizations.Application;
using Organizations.Domain;
using Xunit;

namespace Organizations.UnitTests;

public sealed class StaffAuthorizationTests
{
    private static readonly Guid User = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherUser = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid OrgA = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid OrgB = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid BranchA = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchB = Guid.Parse("30000000-0000-0000-0000-000000000002");

    private static AccessRequest Request(Guid? org = null, Guid? branch = null,
        Capability capability = Capability.WorkspaceRead) => new(User, org ?? OrgA, branch ?? BranchA, 1, capability);

    private static Membership Member(Guid? org = null, Guid? branch = null,
        MembershipStatus status = MembershipStatus.Active, long revision = 1,
        Capability[]? capabilities = null, Capability[]? delegable = null, Guid? user = null) =>
        new(user ?? User, org ?? OrgA, status, revision, [branch ?? BranchA],
            capabilities ?? [Capability.WorkspaceRead], delegable ?? []);

    private static OrganizationAccessSnapshot Snapshot(Guid? org = null, Guid? branch = null) =>
        new(new(org ?? OrgA, OrganizationStatus.Active), new(branch ?? BranchA, org ?? OrgA, true),
            Member(org, branch));

    private sealed class Fixtures : IStaffIdentityReader, IOrganizationAccessReader
    {
        public StaffIdentity? UserState { get; set; } = new(User, true);
        public OrganizationAccessSnapshot? State { get; set; } = Snapshot();
        public Func<Guid, Guid, Guid, ValueTask<OrganizationAccessSnapshot?>>? Read { get; set; }
        public Func<Guid, ValueTask<StaffIdentity?>>? ReadIdentity { get; set; }
        public StaffAuthorization Service => new(this, this);
        public ValueTask<StaffIdentity?> ReadCurrentAsync(Guid userId, CancellationToken cancellationToken) =>
            ReadIdentity is null ? ValueTask.FromResult(UserState) : ReadIdentity(userId);
        public ValueTask<OrganizationAccessSnapshot?> ReadCurrentAsync(Guid userId, Guid organizationId,
            Guid branchId, CancellationToken cancellationToken) =>
            Read is null ? ValueTask.FromResult(State) : Read(userId, organizationId, branchId);
    }

    [Fact]
    public async Task Explicit_grants_produce_action_specific_context()
    {
        var context = Assert.IsType<AuthorizationContext>(await new Fixtures().Service.AuthorizeAsync(Request()));
        Assert.Equal(User, context.UserId);
        Assert.Equal(OrgA, context.OrganizationId);
        Assert.Equal(BranchA, context.BranchId);
        Assert.Equal(1, context.Revision);
        Assert.Equal(Capability.WorkspaceRead, context.Capability);
    }

    [Theory]
    [InlineData("missing-user")]
    [InlineData("disabled-user")]
    [InlineData("wrong-user")]
    [InlineData("missing-snapshot")]
    [InlineData("missing-organization")]
    [InlineData("suspended-organization")]
    [InlineData("closed-organization")]
    [InlineData("unknown-organization-status")]
    [InlineData("foreign-organization")]
    [InlineData("missing-branch")]
    [InlineData("foreign-branch")]
    [InlineData("wrong-branch-owner")]
    [InlineData("disabled-branch")]
    [InlineData("missing-membership")]
    [InlineData("removed-membership")]
    [InlineData("suspended-membership")]
    [InlineData("foreign-membership")]
    [InlineData("wrong-member-user")]
    [InlineData("ungranted-branch")]
    [InlineData("ungranted-capability")]
    [InlineData("stale-revision")]
    public async Task Current_state_must_match_every_authorization_boundary(string scenario)
    {
        var fixtures = new Fixtures();
        var state = fixtures.State!;
        switch (scenario)
        {
            case "missing-user": fixtures.UserState = null; break;
            case "disabled-user": fixtures.UserState = new(User, false); break;
            case "wrong-user": fixtures.UserState = new(OtherUser, true); break;
            case "missing-snapshot": fixtures.State = null; break;
            case "missing-organization": fixtures.State = state with { Organization = null }; break;
            case "suspended-organization": fixtures.State = state with { Organization = new(OrgA, OrganizationStatus.Suspended) }; break;
            case "closed-organization": fixtures.State = state with { Organization = new(OrgA, OrganizationStatus.Closed) }; break;
            case "unknown-organization-status": fixtures.State = state with { Organization = new(OrgA, (OrganizationStatus)999) }; break;
            case "foreign-organization": fixtures.State = state with { Organization = new(OrgB, OrganizationStatus.Active) }; break;
            case "missing-branch": fixtures.State = state with { Branch = null }; break;
            case "foreign-branch": fixtures.State = state with { Branch = new(BranchB, OrgA, true) }; break;
            case "wrong-branch-owner": fixtures.State = state with { Branch = new(BranchA, OrgB, true) }; break;
            case "disabled-branch": fixtures.State = state with { Branch = new(BranchA, OrgA, false) }; break;
            case "missing-membership": fixtures.State = state with { Membership = null }; break;
            case "removed-membership": fixtures.State = state with { Membership = Member(status: MembershipStatus.Removed) }; break;
            case "suspended-membership": fixtures.State = state with { Membership = Member(status: MembershipStatus.Suspended) }; break;
            case "foreign-membership": fixtures.State = state with { Membership = Member(org: OrgB) }; break;
            case "wrong-member-user": fixtures.State = state with { Membership = Member(user: OtherUser) }; break;
            case "ungranted-branch": fixtures.State = state with { Membership = Member(branch: BranchB) }; break;
            case "ungranted-capability": fixtures.State = state with { Membership = Member(capabilities: []) }; break;
            case "stale-revision": fixtures.State = state with { Membership = Member(revision: 2) }; break;
        }
        Assert.Null(await fixtures.Service.AuthorizeAsync(Request()));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("organization")]
    [InlineData("branch")]
    [InlineData("revision")]
    [InlineData("capability")]
    public async Task Missing_or_unknown_context_is_denied_before_reading_grants(string field)
    {
        var fixtures = new Fixtures { Read = (_, _, _) => throw new InvalidOperationException("Must not read") };
        var request = field switch
        {
            "user" => Request() with { UserId = Guid.Empty },
            "organization" => Request() with { OrganizationId = Guid.Empty },
            "branch" => Request() with { BranchId = Guid.Empty },
            "revision" => Request() with { ExpectedRevision = 0 },
            _ => Request() with { Capability = (Capability)999 }
        };
        Assert.Null(await fixtures.Service.AuthorizeAsync(request));
    }

    [Fact]
    public async Task Multiple_memberships_do_not_union_permissions_even_with_same_local_branch_reference()
    {
        var fixtures = new Fixtures
        {
            Read = (_, org, branch) => ValueTask.FromResult<OrganizationAccessSnapshot?>(
                org == OrgB ? Snapshot(OrgB, branch) with
                { Membership = Member(OrgB, branch, capabilities: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate]) }
                : Snapshot(OrgA, branch))
        };
        var service = fixtures.Service;
        Assert.NotNull(await service.AuthorizeAsync(Request(OrgB, BranchA, Capability.DelegationScopeEvaluate)));
        Assert.Null(await service.AuthorizeAsync(Request(capability: Capability.DelegationScopeEvaluate)));
        Assert.NotNull(await service.AuthorizeAsync(Request()));
    }

    [Fact]
    public async Task Later_requests_recheck_current_identity_membership_and_revision()
    {
        var fixtures = new Fixtures();
        var service = fixtures.Service;
        var original = Assert.IsType<AuthorizationContext>(await service.AuthorizeAsync(Request()));
        fixtures.State = Snapshot() with { Membership = Member(revision: 2) };
        Assert.Null(await service.AuthorizeAsync(Request()));
        Assert.NotNull(await service.AuthorizeAsync(Request() with { ExpectedRevision = 2 }));
        fixtures.State = Snapshot() with { Membership = Member(status: MembershipStatus.Removed, revision: 3) };
        Assert.Null(await service.AuthorizeAsync(Request() with { ExpectedRevision = 3 }));
        fixtures.State = Snapshot();
        fixtures.UserState = new(User, false);
        Assert.Null(await service.AuthorizeAsync(Request()));
        Assert.Equal(1, original.Revision); // Old evidence is immutable, not a credential for another call.
    }

    [Theory]
    [InlineData("allowed", true)]
    [InlineData("self", false)]
    [InlineData("empty-recipient", false)]
    [InlineData("foreign-organization", false)]
    [InlineData("foreign-branch", false)]
    [InlineData("administration", false)]
    [InlineData("unknown-capability", false)]
    [InlineData("no-delegation-permission", false)]
    [InlineData("no-delegable-grant", false)]
    [InlineData("not-held", false)]
    [InlineData("wrong-action", false)]
    [InlineData("stale", false)]
    public async Task Delegation_is_explicit_bounded_and_cannot_elevate(string scenario, bool expected)
    {
        var member = Member(capabilities: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate],
            delegable: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate]);
        var request = Request(capability: Capability.DelegationScopeEvaluate);
        var recipient = scenario == "self" ? User : scenario == "empty-recipient" ? Guid.Empty : OtherUser;
        var organization = scenario == "foreign-organization" ? OrgB : OrgA;
        var branch = scenario == "foreign-branch" ? BranchB : BranchA;
        var capability = scenario == "administration" ? Capability.DelegationScopeEvaluate
            : scenario == "unknown-capability" ? (Capability)999 : Capability.WorkspaceRead;
        if (scenario == "no-delegation-permission") member = Member(delegable: [Capability.WorkspaceRead]);
        if (scenario == "no-delegable-grant") member = Member(capabilities: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate]);
        if (scenario == "not-held") member = Member(capabilities: [Capability.DelegationScopeEvaluate], delegable: [Capability.WorkspaceRead]);
        if (scenario == "wrong-action") request = Request();
        if (scenario == "stale") request = request with { ExpectedRevision = 2 };
        var fixtures = new Fixtures { State = Snapshot() with { Membership = member } };
        var evaluation = await fixtures.Service.EvaluateActorDelegationScopeAsync(request, recipient, organization, branch, capability);
        Assert.Equal(expected, evaluation.IsActorScopeAllowed);
    }

    [Fact]
    public async Task Two_independently_granted_branches_in_one_organization_are_accessible()
    {
        var membership = new Membership(User, OrgA, MembershipStatus.Active, 1,
            [BranchA, BranchB], [Capability.WorkspaceRead], []);
        var fixtures = new Fixtures
        {
            Read = (_, org, branch) => ValueTask.FromResult<OrganizationAccessSnapshot?>(
                Snapshot(org, branch) with { Membership = membership })
        };
        var service = fixtures.Service;
        var a = Assert.IsType<AuthorizationContext>(await service.AuthorizeAsync(Request()));
        var b = Assert.IsType<AuthorizationContext>(await service.AuthorizeAsync(Request(branch: BranchB)));
        Assert.Equal(OrgA, a.OrganizationId);
        Assert.Equal(OrgA, b.OrganizationId);
        Assert.Equal(BranchA, a.BranchId);
        Assert.Equal(BranchB, b.BranchId);
        Assert.Null(await service.AuthorizeAsync(Request(branch: Guid.Parse("30000000-0000-0000-0000-000000000003")))); // An ungranted reference remains denied.
    }

    [Fact]
    public async Task Removing_membership_in_A_preserves_independent_membership_in_B()
    {
        var membershipA = Member();
        var membershipB = Member(OrgB, BranchB);
        var fixtures = new Fixtures
        {
            Read = (_, org, branch) => ValueTask.FromResult<OrganizationAccessSnapshot?>(
                Snapshot(org, branch) with { Membership = org == OrgA ? membershipA : membershipB })
        };
        var service = fixtures.Service;
        Assert.NotNull(await service.AuthorizeAsync(Request()));
        Assert.NotNull(await service.AuthorizeAsync(Request(OrgB, BranchB)));
        membershipA = Member(status: MembershipStatus.Removed, revision: 2);
        Assert.Null(await service.AuthorizeAsync(Request() with { ExpectedRevision = 2 }));
        var b = Assert.IsType<AuthorizationContext>(await service.AuthorizeAsync(Request(OrgB, BranchB)));
        Assert.Equal(OrgB, b.OrganizationId);
        Assert.Equal(1, b.Revision);
    }

    [Theory]
    [InlineData("disabled-actor")]
    [InlineData("removed-membership")]
    [InlineData("scope-capability-removed")]
    [InlineData("workspace-capability-removed")]
    [InlineData("delegable-grant-removed")]
    public async Task Actor_delegation_scope_rechecks_reductions_after_a_success(string change)
    {
        var fixtures = new Fixtures
        {
            State = Snapshot() with
            {
                Membership = Member(capabilities: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate],
                    delegable: [Capability.WorkspaceRead])
            }
        };
        var service = fixtures.Service;
        var request = Request(capability: Capability.DelegationScopeEvaluate);
        Assert.True((await service.EvaluateActorDelegationScopeAsync(request, OtherUser, OrgA, BranchA,
            Capability.WorkspaceRead)).IsActorScopeAllowed);
        if (change == "disabled-actor") fixtures.UserState = new(User, false);
        var capabilities = change == "scope-capability-removed" ? new[] { Capability.WorkspaceRead }
            : change == "workspace-capability-removed" ? new[] { Capability.DelegationScopeEvaluate }
            : new[] { Capability.WorkspaceRead, Capability.DelegationScopeEvaluate };
        fixtures.State = Snapshot() with
        {
            Membership = Member(status: change == "removed-membership" ? MembershipStatus.Removed : MembershipStatus.Active,
                revision: 2, capabilities: capabilities,
                delegable: change == "delegable-grant-removed" ? [] : [Capability.WorkspaceRead])
        };
        // Use the new revision so denial must reflect current authority, not just a stale request.
        Assert.False((await service.EvaluateActorDelegationScopeAsync(request with { ExpectedRevision = 2 }, OtherUser,
            OrgA, BranchA, Capability.WorkspaceRead)).IsActorScopeAllowed);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("disabled")]
    [InlineData("foreign-membership")]
    [InlineData("no-branch-grant")]
    public async Task Actor_scope_result_does_not_verify_candidate_recipient_eligibility(string candidateState)
    {
        var actorSnapshot = Snapshot() with
        {
            Membership = Member(capabilities: [Capability.WorkspaceRead, Capability.DelegationScopeEvaluate],
                delegable: [Capability.WorkspaceRead])
        };
        StaffIdentity? candidateIdentity = candidateState == "unknown" ? null : new(OtherUser, candidateState != "disabled");
        OrganizationAccessSnapshot? candidateSnapshot = candidateState == "unknown" ? null
            : Snapshot() with
            {
                Membership = Member(user: OtherUser,
                    org: candidateState == "foreign-membership" ? OrgB : OrgA,
                    branch: candidateState == "no-branch-grant" ? BranchB : BranchA)
            };
        var identityReads = new List<Guid>();
        var membershipReads = new List<Guid>();
        var fixtures = new Fixtures
        {
            State = actorSnapshot,
            ReadIdentity = user =>
            {
                identityReads.Add(user);
                return ValueTask.FromResult(user == User ? new StaffIdentity(User, true) : candidateIdentity);
            },
            Read = (user, _, _) =>
            {
                membershipReads.Add(user);
                return ValueTask.FromResult(user == User ? actorSnapshot : candidateSnapshot);
            }
        };
        var evaluation = await fixtures.Service.EvaluateActorDelegationScopeAsync(
            Request(capability: Capability.DelegationScopeEvaluate), OtherUser, OrgA, BranchA, Capability.WorkspaceRead);
        Assert.True(evaluation.IsActorScopeAllowed); // This explicitly does not authorize a grant to this candidate.
        Assert.Equal(new[] { User }, identityReads);
        Assert.Equal(new[] { User }, membershipReads);
        Assert.Same(actorSnapshot, fixtures.State); // Evaluation has no membership writer or mutation.
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("actor-scope")]
    [InlineData("actor-scope-wrong-action")]
    public async Task Pre_cancelled_requests_never_read_or_return_a_success(string operation)
    {
        var fixtures = new Fixtures
        {
            ReadIdentity = _ => throw new InvalidOperationException("Must not read identity"),
            Read = (_, _, _) => throw new InvalidOperationException("Must not read grants")
        };
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var service = fixtures.Service;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (operation == "authorization") await service.AuthorizeAsync(Request(), cancelled.Token);
            else await service.EvaluateActorDelegationScopeAsync(
                Request(capability: operation == "actor-scope" ? Capability.DelegationScopeEvaluate : Capability.WorkspaceRead),
                OtherUser, OrgA, BranchA, Capability.WorkspaceRead, cancelled.Token);
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Reader_failures_propagate_without_a_successful_decision(bool actorScope, bool identityFailure)
    {
        var expected = new InvalidOperationException("Synthetic reader failure");
        var fixtures = new Fixtures
        {
            ReadIdentity = user => identityFailure ? throw expected : ValueTask.FromResult<StaffIdentity?>(new(user, true)),
            Read = (_, _, _) => throw expected
        };
        var service = fixtures.Service;
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (actorScope) await service.EvaluateActorDelegationScopeAsync(Request(capability: Capability.DelegationScopeEvaluate),
                OtherUser, OrgA, BranchA, Capability.WorkspaceRead);
            else await service.AuthorizeAsync(Request());
        });
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task Interleaved_requests_on_one_service_keep_independent_immutable_contexts()
    {
        var enteredA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var fixtures = new Fixtures
        {
            Read = async (_, org, branch) =>
            {
                if (org == OrgA) { enteredA.SetResult(); await releaseA.Task.WaitAsync(timeout.Token); }
                return Snapshot(org, branch);
            }
        };
        var service = fixtures.Service;
        var pendingA = service.AuthorizeAsync(Request(), timeout.Token).AsTask();
        try
        {
            await enteredA.Task.WaitAsync(timeout.Token);
            var b = Assert.IsType<AuthorizationContext>(await service.AuthorizeAsync(Request(OrgB, BranchB), timeout.Token)
                .AsTask().WaitAsync(timeout.Token));
            releaseA.TrySetResult();
            var a = Assert.IsType<AuthorizationContext>(await pendingA.WaitAsync(timeout.Token));
            Assert.Equal(OrgA, a.OrganizationId);
            Assert.Equal(BranchA, a.BranchId);
            Assert.Equal(OrgB, b.OrganizationId);
            Assert.Equal(BranchB, b.BranchId);
            Assert.NotSame(a, b);
        }
        finally
        {
            releaseA.TrySetResult();
            timeout.Cancel();
            // Observe the released request even if an earlier assertion failed, with bounded cleanup.
            try { await pendingA.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
        }
    }

    [Fact]
    public void Membership_copies_grants_and_rejects_malformed_grants()
    {
        var branches = new HashSet<Guid> { BranchA };
        var grants = new HashSet<Capability> { Capability.WorkspaceRead };
        var delegable = new HashSet<Capability>();
        var member = new Membership(User, OrgA, MembershipStatus.Active, 1, branches, grants, delegable);
        branches.Add(BranchB);
        grants.Add(Capability.DelegationScopeEvaluate);
        delegable.Add(Capability.WorkspaceRead);
        Assert.DoesNotContain(BranchB, member.BranchIds);
        Assert.DoesNotContain(Capability.DelegationScopeEvaluate, member.Capabilities);
        Assert.Empty(member.DelegableCapabilities);
        Assert.Throws<ArgumentException>(() => Member(revision: 0));
        Assert.Throws<ArgumentException>(() => Member(status: (MembershipStatus)999));
        Assert.Throws<ArgumentException>(() => Member(capabilities: [(Capability)999]));
        Assert.Throws<ArgumentException>(() => Member(delegable: [(Capability)999]));
        Assert.Throws<ArgumentException>(() => Member(branch: Guid.Empty));
    }
}
