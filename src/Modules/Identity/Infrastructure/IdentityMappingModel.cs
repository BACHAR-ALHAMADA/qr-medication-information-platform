using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public static class IdentityMappingModel
{
    // Match the Application contract's control/whitespace rejection without normalizing identities.
    internal const string SubjectCheck = "octet_length(subject) BETWEEN 1 AND 1024 AND subject !~ U&'[\\0001-\\001F\\007F-\\009F]' AND btrim(subject, U&'\\0020\\00A0\\1680\\2000\\2001\\2002\\2003\\2004\\2005\\2006\\2007\\2008\\2009\\200A\\2028\\2029\\202F\\205F\\3000') <> ''";
    internal const string IssuerCheck = "octet_length(issuer) BETWEEN 1 AND 1024 AND issuer ~* '^https://[^/?#@[:space:]]+(/[^?#[:space:]]*)?$' AND issuer !~ U&'[\\0001-\\0020\\007F-\\00A0\\1680\\2000-\\200A\\2028\\2029\\202F\\205F\\3000]'";

    public static void Configure(ModelBuilder model)
    {
        var issuer = model.SharedTypeEntity<Dictionary<string, object>>("AuthenticationIssuer");
        issuer.ToTable("authentication_issuers", "identity", table =>
        {
            table.HasCheckConstraint("ck_issuer_id", "issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_issuer_value", IssuerCheck);
            table.HasCheckConstraint("ck_issuer_revision", "revision > 0");
        });
        issuer.IndexerProperty<Guid>("issuer_id").HasDefaultValueSql("gen_random_uuid()");
        issuer.IndexerProperty<string>("issuer").HasColumnType("text").UseCollation("C").IsRequired();
        issuer.IndexerProperty<bool>("is_enabled").HasDefaultValue(false);
        issuer.IndexerProperty<long>("revision").HasDefaultValue(1L).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        issuer.IndexerProperty<DateTime>("created_at").HasDefaultValueSql("statement_timestamp()").ValueGeneratedOnAdd();
        issuer.IndexerProperty<DateTime>("updated_at").HasDefaultValueSql("statement_timestamp()").ValueGeneratedOnAddOrUpdate();
        issuer.HasKey("issuer_id");
        issuer.HasIndex("issuer").IsUnique();

        var mapping = model.SharedTypeEntity<Dictionary<string, object>>("ExternalStaffIdentity");
        mapping.ToTable("external_staff_identities", "identity", table =>
        {
            table.HasCheckConstraint("ck_mapping_ids", "mapping_id <> '00000000-0000-0000-0000-000000000000'::uuid AND issuer_id <> '00000000-0000-0000-0000-000000000000'::uuid AND user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            table.HasCheckConstraint("ck_mapping_subject", SubjectCheck);
            table.HasCheckConstraint("ck_mapping_state", "state IN ('active','disabled','unlinked')");
            table.HasCheckConstraint("ck_mapping_revision", "revision > 0");
        });
        mapping.IndexerProperty<Guid>("mapping_id").HasDefaultValueSql("gen_random_uuid()");
        mapping.IndexerProperty<Guid>("issuer_id");
        mapping.IndexerProperty<Guid>("user_id");
        mapping.IndexerProperty<string>("subject").HasColumnType("text").UseCollation("C").IsRequired();
        mapping.IndexerProperty<string>("state").IsRequired().HasDefaultValue("disabled");
        mapping.IndexerProperty<long>("revision").HasDefaultValue(1L).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        mapping.IndexerProperty<DateTime>("created_at").HasDefaultValueSql("statement_timestamp()").ValueGeneratedOnAdd();
        mapping.IndexerProperty<DateTime>("updated_at").HasDefaultValueSql("statement_timestamp()").ValueGeneratedOnAddOrUpdate();
        mapping.HasKey("mapping_id");
        mapping.HasIndex("issuer_id", "subject").IsUnique();
        mapping.HasIndex("user_id");
        mapping.HasOne("AuthenticationIssuer", null).WithMany().HasForeignKey("issuer_id").OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne("StaffUser", null).WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Restrict);
    }
}
