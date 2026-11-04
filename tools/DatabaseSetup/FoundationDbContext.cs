using Identity.Infrastructure;
using Organizations.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
namespace DatabaseSetup;

// Explicit offline migration composition only. Never registered in the API.
public sealed class FoundationDbContext(DbContextOptions<FoundationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        IdentityPersistenceModel.Configure(model);
        OrganizationsPersistenceModel.Configure(model);
        // The approved cross-module FK is migration SQL only, not an EF navigation.
    }
}
public sealed class DesignFactory : IDesignTimeDbContextFactory<FoundationDbContext>
{
    public FoundationDbContext CreateDbContext(string[] args) => LocalDatabase.CreateContext(LocalDatabase.ConnectionString());
}
public static class LocalDatabase
{
    public static bool IsCi => Environment.GetEnvironmentVariable("QR_DATABASE_MODE") == "ci";
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        is { Length: > 0 } value ? value : throw new InvalidOperationException("Missing disposable database configuration.");
    private static NpgsqlConnectionStringBuilder CiConnection(string role, string password) => new()
    {
        Host="127.0.0.1", Port=int.Parse(Required("QR_DATABASE_PORT")), Database="qr_medication_ci",
        Username=role, Password=password, IncludeErrorDetail=false, Timeout=10, CommandTimeout=30
    };
    public static string ConnectionString()
    {
        if (Environment.GetEnvironmentVariable("QR_DATABASE_MODE") is { Length: > 0 } mode && mode != "ci")
            throw new InvalidOperationException("Unknown database configuration mode.");
        if (IsCi) return CiConnection("qr_ci_admin", Required("QR_DATABASE_ADMIN_PASSWORD")).ConnectionString;
        if (Environment.GetEnvironmentVariable("CI") == "true")
            throw new InvalidOperationException("CI requires explicit disposable database mode.");
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root != null && !File.Exists(Path.Combine(root.FullName, "global.json"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Run from the repository.");
        var values = File.ReadAllLines(Path.Combine(root.FullName, "infrastructure/development/.env"))
            .Where(line => !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
        if (values["POSTGRES_DB"] != "qr_medication_dev" || values["POSTGRES_PORT"] != "55432")
            throw new InvalidOperationException("Only the approved local development database is allowed.");
        return new NpgsqlConnectionStringBuilder { Host="127.0.0.1", Port=55432,
            Database="qr_medication_dev", Username=values["POSTGRES_USER"], Password=values["POSTGRES_PASSWORD"],
            IncludeErrorDetail=false, Timeout=10, CommandTimeout=30 }.ConnectionString;
    }
    public static string RoleConnectionString(string role)
    {
        if (role is not ("qr_reader" or "qr_setup" or "qr_migrator")) throw new ArgumentException("Unknown local role.");
        if (IsCi) return CiConnection(role, Required("QR_DATABASE_" + role.ToUpperInvariant() + "_PASSWORD")).ConnectionString;
        var values = File.ReadAllLines(RoleFile()).Where(line => line.Contains('=') && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);
        return new NpgsqlConnectionStringBuilder(ConnectionString())
            { Username=role, Password=values[role], ApplicationName="qr-local-synthetic-harness" }.ConnectionString;
    }
    private static string RoleFile()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root != null && !File.Exists(Path.Combine(root.FullName, "global.json"))) root=root.Parent;
        if (root == null) throw new InvalidOperationException("Run from the repository.");
        return Path.Combine(root.FullName,"infrastructure/development/.env.roles");
    }
    public static async Task ProvisionLocalRolePasswordsAsync()
    {
        var path=IsCi ? null : RoleFile();
        if (path != null && !File.Exists(path))
        {
            var lines = new[] {"qr_reader","qr_setup","qr_migrator"}.Select(role =>
                role + "=" + Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            using var file = new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            using var writer = new StreamWriter(file);
            writer.WriteLine("# Local synthetic testing credentials only. Never share this file.");
            foreach (var line in lines) writer.WriteLine(line);
        }
        await using var admin = new NpgsqlConnection(ConnectionString());
        await admin.OpenAsync();
        // Keep password statements and parameters out of this tool's/server's diagnostic output.
        await new NpgsqlCommand("SET log_statement='none'; SET log_min_error_statement='panic'; SET log_parameter_max_length_on_error=0",admin).ExecuteNonQueryAsync();
        foreach (var role in new[] {"qr_reader","qr_setup","qr_migrator"})
        {
            var secret=new NpgsqlConnectionStringBuilder(RoleConnectionString(role)).Password!;
            await using var format=new NpgsqlCommand("SELECT format('ALTER ROLE %I PASSWORD %L', $1::text, $2::text)",admin);
            format.Parameters.AddWithValue(role);format.Parameters.AddWithValue(secret);
            var statement=(string)(await format.ExecuteScalarAsync())!;
            await using var change=new NpgsqlCommand(statement,admin);
            await change.ExecuteNonQueryAsync();
        }
    }
    public static FoundationDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<FoundationDbContext>().UseNpgsql(connectionString,
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", "foundation")).Options);
}
