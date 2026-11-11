using DatabaseSetup;
using Npgsql;
using Xunit;
namespace Foundation.PersistenceTests;

public sealed class RlsFixture : IAsyncLifetime
{
    public Guid A { get; }=Guid.NewGuid();
    public Guid B { get; }=Guid.NewGuid();
    public Guid Alice { get; }=Guid.NewGuid();
    public Guid Bob { get; }=Guid.NewGuid();
    public Guid Disabled { get; }=Guid.NewGuid();
    public Guid Inactive { get; }=Guid.NewGuid();
    public Guid Removed { get; }=Guid.NewGuid();
    public Guid AOnly { get; }=Guid.NewGuid();
    public Guid Branch { get; }=Guid.NewGuid();
    public Guid Unassigned { get; }=Guid.NewGuid();
    public Guid DisabledBranch { get; }=Guid.NewGuid();
    public NpgsqlDataSource Reader { get; }=NpgsqlDataSource.Create(LocalDatabase.RoleConnectionString("qr_reader"));
    public static async Task<int> Exec(NpgsqlConnection c,string sql,params object[] values)
    {
        await using var command=new NpgsqlCommand(sql,c);
        foreach(var value in values) command.Parameters.AddWithValue(value);
        return await command.ExecuteNonQueryAsync();
    }
    public static async Task<T> Scalar<T>(NpgsqlConnection c,string sql,params object[] values)
    {
        await using var command=new NpgsqlCommand(sql,c);
        foreach(var value in values) command.Parameters.AddWithValue(value);
        return (T)(await command.ExecuteScalarAsync())!;
    }
    public async Task Setup(string sql,params object[] values)
    {
        await using var c=new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_setup"));
        await c.OpenAsync();
        await Exec(c,sql,values);
    }
    public async Task InitializeAsync()
    {
        await using var c=new NpgsqlConnection(LocalDatabase.RoleConnectionString("qr_setup"));
        await c.OpenAsync();
        await using var tx=await c.BeginTransactionAsync();
        foreach(var actor in new[]{Alice,Bob,Disabled,Inactive,Removed,AOnly})
            await Exec(c,"INSERT INTO identity.staff_users VALUES ($1,$2)",actor,actor!=Disabled);
        foreach(var org in new[]{A,B})
        {
            await Exec(c,"INSERT INTO organizations.organizations VALUES ($1,'RLS synthetic fixture','active')",org);
            await Exec(c,"INSERT INTO organizations.branches VALUES ($1,$2,'Synthetic shared reference',true)",org,Branch);
        }
        await Exec(c,"INSERT INTO organizations.branches VALUES ($1,$2,'Synthetic unassigned',true),($1,$3,'Synthetic disabled',false)",A,Unassigned,DisabledBranch);
        foreach(var pair in new[]{(A,Alice),(B,Alice),(B,Bob),(A,Disabled),(A,Inactive),(A,Removed),(A,AOnly)})
        {
            var state=pair.Item2==Inactive ? "suspended" : pair.Item2==Removed ? "removed" : "active";
            await Exec(c,"INSERT INTO organizations.memberships VALUES ($1,$2,1,$3)",pair.Item1,pair.Item2,state);
            await Exec(c,"INSERT INTO organizations.membership_branch_grants VALUES ($1,$2,$3)",pair.Item1,pair.Item2,Branch);
            await Exec(c,"INSERT INTO organizations.membership_capabilities VALUES ($1,$2,'workspace.read',false)",pair.Item1,pair.Item2);
        }
        await Exec(c,"INSERT INTO organizations.membership_branch_grants VALUES ($1,$2,$3)",A,Alice,DisabledBranch);
        await tx.CommitAsync();
    }
    public async Task DisposeAsync()
    {
        await Reader.DisposeAsync();
        // Explicit privileged fixture teardown: only UUIDs allocated by this harness.
        // No TRUNCATE, DROP, unrelated row deletion or persistent trigger disablement.
        await using var c=new NpgsqlConnection(LocalDatabase.ConnectionString());
        await c.OpenAsync();
        await using var tx=await c.BeginTransactionAsync();
        foreach(var table in new[]{"membership_capabilities","membership_branch_grants"})
            await Exec(c,$"DELETE FROM organizations.{table} WHERE organization_id=ANY($1)",new[]{A,B});
        await Exec(c,"ALTER TABLE organizations.memberships DISABLE TRIGGER membership_revision");
        await Exec(c,"DELETE FROM organizations.memberships WHERE organization_id=ANY($1)",new[]{A,B});
        await Exec(c,"ALTER TABLE organizations.memberships ENABLE TRIGGER membership_revision");
        await Exec(c,"DELETE FROM organizations.branches WHERE organization_id=ANY($1)",new[]{A,B});
        await Exec(c,"DELETE FROM organizations.organizations WHERE organization_id=ANY($1)",new[]{A,B});
        await Exec(c,"DELETE FROM identity.staff_users WHERE user_id=ANY($1)",new[]{Alice,Bob,Disabled,Inactive,Removed,AOnly});
        await tx.CommitAsync();
    }
}