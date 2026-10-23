using Identity.Application;
using Organizations.Domain;

namespace Organizations.Application;

public sealed record AccessRequest(Guid UserId, Guid OrganizationId, Guid BranchId,
    long ExpectedRevision, Capability Capability);

// Actor-scope evidence only, not recipient authorization or permission to execute a grant.
public sealed record ActorDelegationScopeEvaluation(bool IsActorScopeAllowed);

// The reader must return a coherent current snapshot; no login-time or positive-grant cache.
public sealed record OrganizationAccessSnapshot(PharmacyOrganization? Organization, Branch? Branch, Membership? Membership);

public interface IOrganizationAccessReader
{
    ValueTask<OrganizationAccessSnapshot?> ReadCurrentAsync(Guid userId, Guid organizationId,
        Guid branchId, CancellationToken cancellationToken);
}

public interface IStaffAuthorization
{
    ValueTask<AuthorizationContext?> AuthorizeAsync(AccessRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Evaluates current actor scope only. Recipient existence, organization membership and status
    /// are not verified. No grant is executed. A nonempty candidate other than the actor is required.
    /// </summary>
    ValueTask<ActorDelegationScopeEvaluation> EvaluateActorDelegationScopeAsync(AccessRequest request, Guid candidateRecipientUserId, Guid targetOrganizationId,
        Guid targetBranchId, Capability capability, CancellationToken cancellationToken = default);
}

public sealed class StaffAuthorization(IStaffIdentityReader identities, IOrganizationAccessReader organizations)
    : IStaffAuthorization
{
    public async ValueTask<AuthorizationContext?> AuthorizeAsync(AccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var state = await ReadAuthorizedAsync(request, cancellationToken);
        return state.Context;
    }

    public async ValueTask<ActorDelegationScopeEvaluation> EvaluateActorDelegationScopeAsync(AccessRequest request, Guid candidateRecipientUserId,
        Guid targetOrganizationId, Guid targetBranchId, Capability capability,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Capability != Capability.DelegationScopeEvaluate) return new(false);
        var state = await ReadAuthorizedAsync(request, cancellationToken);
        return new(state.Context is not null && state.Membership is not null
            && AuthorizationPolicy.IsActorDelegationScopeAllowed(state.Context, state.Membership, candidateRecipientUserId,
                targetOrganizationId, targetBranchId, capability));
    }

    private async ValueTask<(AuthorizationContext? Context, Membership? Membership)> ReadAuthorizedAsync(
        AccessRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.UserId == Guid.Empty || request.OrganizationId == Guid.Empty || request.BranchId == Guid.Empty
            || request.ExpectedRevision < 1 || !Enum.IsDefined(request.Capability)) return (null, null);
        var user = await identities.ReadCurrentAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsEnabled || user.UserId != request.UserId) return (null, null);
        var state = await organizations.ReadCurrentAsync(request.UserId, request.OrganizationId,
            request.BranchId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (state is null) return (null, null);
        var context = AuthorizationPolicy.Evaluate(request.UserId, request.OrganizationId, request.BranchId,
            request.ExpectedRevision, request.Capability, state.Organization, state.Branch, state.Membership);
        return (context, state.Membership);
    }
}
