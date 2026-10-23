namespace Organizations.Domain;

// Evidence for one evaluated action, never a reusable bearer credential.
public sealed class AuthorizationContext
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public Guid BranchId { get; }
    public long Revision { get; }
    public Capability Capability { get; }

    internal AuthorizationContext(Membership membership, Branch branch, Capability capability)
    {
        UserId = membership.UserId;
        OrganizationId = membership.OrganizationId;
        BranchId = branch.Id;
        Revision = membership.Revision;
        Capability = capability;
    }
}

public static class AuthorizationPolicy
{
    public static AuthorizationContext? Evaluate(Guid userId, Guid organizationId, Guid branchId,
        long expectedRevision, Capability capability, PharmacyOrganization? organization,
        Branch? branch, Membership? membership)
    {
        if (userId == Guid.Empty || organizationId == Guid.Empty || branchId == Guid.Empty
            || expectedRevision < 1 || !Enum.IsDefined(capability)
            || organization is null || organization.Id != organizationId
            || organization.Status != OrganizationStatus.Active
            || branch is null || branch.Id != branchId || branch.OrganizationId != organizationId || !branch.IsEnabled
            || membership is null || membership.UserId != userId || membership.OrganizationId != organizationId
            || membership.Status != MembershipStatus.Active || membership.Revision != expectedRevision
            || !membership.BranchIds.Contains(branchId) || !membership.Capabilities.Contains(capability))
            return null;

        return new AuthorizationContext(membership, branch, capability);
    }

    // Checks only the already-evaluated actor's scope. Candidate recipient existence, organization
    // membership and status are NOT verified. No grant executes; administration cannot be delegated.
    public static bool IsActorDelegationScopeAllowed(AuthorizationContext context, Membership currentMembership,
        Guid candidateRecipientUserId, Guid targetOrganizationId, Guid targetBranchId, Capability requestedCapability) =>
        context.Capability == Capability.DelegationScopeEvaluate
        && context.UserId == currentMembership.UserId
        && context.OrganizationId == currentMembership.OrganizationId
        && context.Revision == currentMembership.Revision
        && currentMembership.Status == MembershipStatus.Active
        && candidateRecipientUserId != Guid.Empty && candidateRecipientUserId != context.UserId
        && targetOrganizationId == context.OrganizationId && targetBranchId == context.BranchId
        && currentMembership.BranchIds.Contains(targetBranchId)
        && currentMembership.Capabilities.Contains(Capability.DelegationScopeEvaluate)
        && requestedCapability == Capability.WorkspaceRead
        && currentMembership.Capabilities.Contains(requestedCapability)
        && currentMembership.DelegableCapabilities.Contains(requestedCapability);
}
