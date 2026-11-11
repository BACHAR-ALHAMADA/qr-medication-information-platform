using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Organizations.Infrastructure;

public sealed class OrganizationsDbContext(DbContextOptions<OrganizationsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model) => OrganizationsPersistenceModel.Configure(model);
}

public static class OrganizationsPersistenceModel
{
    public static void Configure(ModelBuilder model)
    {
        var org = Table(model, "Organization", "organizations", "organization_id");
        org.IndexerProperty<string>("status").IsRequired();
        org.IndexerProperty<string>("display_name").HasMaxLength(160).IsRequired();
        org.ToTable("organizations", "organizations", t => t.HasCheckConstraint("ck_org_status", "status IN ('active','suspended','closed')"));
        var branch = Table(model, "Branch", "branches", "organization_id", "branch_id");
        branch.IndexerProperty<bool>("is_enabled");
        branch.IndexerProperty<string>("display_name").HasMaxLength(160).IsRequired();
        branch.HasOne("Organization", null).WithMany().HasForeignKey("organization_id").OnDelete(DeleteBehavior.Restrict);
        var member = Table(model, "Membership", "memberships", "organization_id", "user_id");
        member.IndexerProperty<string>("status").IsRequired();
        member.IndexerProperty<long>("authorization_revision").IsConcurrencyToken();
        member.HasIndex("user_id", "organization_id");
        member.HasOne("Organization", null).WithMany().HasForeignKey("organization_id").OnDelete(DeleteBehavior.Restrict);
        member.ToTable("memberships", "organizations", t => {
            t.HasCheckConstraint("ck_membership_status", "status IN ('active','suspended','removed')");
            t.HasCheckConstraint("ck_membership_revision", "authorization_revision > 0");
        });
        var grants = Table(model, "BranchGrant", "membership_branch_grants", "organization_id", "user_id", "branch_id");
        grants.HasOne("Membership", null).WithMany().HasForeignKey("organization_id", "user_id").OnDelete(DeleteBehavior.Restrict);
        grants.HasOne("Branch", null).WithMany().HasForeignKey("organization_id", "branch_id").OnDelete(DeleteBehavior.Restrict);
        grants.HasIndex("organization_id", "branch_id", "user_id");
        var codes = model.SharedTypeEntity<Dictionary<string, object>>("Capability");
        codes.ToTable("capabilities", "organizations", t => t.HasCheckConstraint("ck_capability_code", "capability_code IN ('workspace.read','delegation.scope.evaluate')"));
        codes.IndexerProperty<string>("capability_code").IsRequired();
        codes.HasKey("capability_code");
        codes.HasData(new Dictionary<string, object>{{"capability_code", "workspace.read"}}, new Dictionary<string, object>{{"capability_code", "delegation.scope.evaluate"}});
        var caps = Table(model, "MembershipCapability", "membership_capabilities", "organization_id", "user_id");
        caps.IndexerProperty<string>("capability_code").IsRequired();
        caps.IndexerProperty<bool>("is_delegable");
        caps.HasKey("organization_id", "user_id", "capability_code");
        caps.HasOne("Membership", null).WithMany().HasForeignKey("organization_id", "user_id").OnDelete(DeleteBehavior.Restrict);
        caps.HasOne("Capability", null).WithMany().HasForeignKey("capability_code").OnDelete(DeleteBehavior.Restrict);
        caps.ToTable("membership_capabilities", "organizations", t => t.HasCheckConstraint("ck_delegable", "NOT is_delegable OR capability_code = 'workspace.read'"));
    }
    private static EntityTypeBuilder<Dictionary<string, object>> Table(ModelBuilder model, string entity, string table, params string[] keys)
    {
        var builder = model.SharedTypeEntity<Dictionary<string, object>>(entity);
        builder.ToTable(table, "organizations", t => {
            foreach (var key in keys)
                t.HasCheckConstraint($"ck_{table}_{key}_nonempty", $"{key} <> '00000000-0000-0000-0000-000000000000'::uuid");
        });
        foreach (var key in keys) builder.IndexerProperty<Guid>(key).ValueGeneratedNever();
        builder.HasKey(keys);
        return builder;
    }
}
