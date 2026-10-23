using System.Collections.Frozen;

namespace Organizations.Domain;

public enum OrganizationStatus { Active, Suspended, Closed }
public enum MembershipStatus { Active, Suspended, Removed }
public enum Capability { WorkspaceRead, DelegationScopeEvaluate }

public sealed record PharmacyOrganization(Guid Id, OrganizationStatus Status);
public sealed record Branch(Guid Id, Guid OrganizationId, bool IsEnabled);

public sealed class Membership
{
    public Guid UserId { get; }
    public Guid OrganizationId { get; }
    public MembershipStatus Status { get; }
    public long Revision { get; }
    public IReadOnlySet<Guid> BranchIds { get; }
    public IReadOnlySet<Capability> Capabilities { get; }
    public IReadOnlySet<Capability> DelegableCapabilities { get; }

    public Membership(Guid userId, Guid organizationId, MembershipStatus status, long revision,
        IEnumerable<Guid> branchIds, IEnumerable<Capability> capabilities,
        IEnumerable<Capability> delegableCapabilities)
    {
        if (userId == Guid.Empty || organizationId == Guid.Empty || revision < 1 || !Enum.IsDefined(status))
            throw new ArgumentException("Invalid membership identity, status or revision.");
        UserId = userId;
        OrganizationId = organizationId;
        Status = status;
        Revision = revision;
        BranchIds = branchIds.ToFrozenSet();
        Capabilities = capabilities.ToFrozenSet();
        DelegableCapabilities = delegableCapabilities.ToFrozenSet();
        if (BranchIds.Contains(Guid.Empty) || Capabilities.Any(c => !Enum.IsDefined(c))
            || DelegableCapabilities.Any(c => !Enum.IsDefined(c)))
            throw new ArgumentException("Invalid membership grants.");
    }
}
