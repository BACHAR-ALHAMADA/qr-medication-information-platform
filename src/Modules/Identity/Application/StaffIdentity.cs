namespace Identity.Application;

// Internal, trusted application contract. This is not an authenticated browser identity.
public sealed record StaffIdentity(Guid UserId, bool IsEnabled);

public interface IStaffIdentityReader
{
    ValueTask<StaffIdentity?> ReadCurrentAsync(Guid userId, CancellationToken cancellationToken);
}
