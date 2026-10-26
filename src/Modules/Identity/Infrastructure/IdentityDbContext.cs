using Microsoft.EntityFrameworkCore;
namespace Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model) => IdentityPersistenceModel.Configure(model);
}

// Intentional persistence-only composition contract; no Domain/Application dependency.
public static class IdentityPersistenceModel
{
    public static void Configure(ModelBuilder model)
    {
        var user = model.SharedTypeEntity<Dictionary<string, object>>("StaffUser");
        user.ToTable("staff_users", "identity", t => t.HasCheckConstraint("ck_staff_user_nonempty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid"));
        user.IndexerProperty<Guid>("user_id").ValueGeneratedNever();
        user.IndexerProperty<bool>("is_enabled");
        user.HasKey("user_id");
    }
}
