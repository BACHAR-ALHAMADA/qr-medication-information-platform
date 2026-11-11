using DatabaseSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
try
{
    if (args.Length != 1 || args[0] is not ("script" or "rls-script" or "mapping-script" or "apply" or "bootstrap" or "ci-bootstrap"))
        throw new ArgumentException("Use script, rls-script, mapping-script, bootstrap, ci-bootstrap or apply.");
    var bootstrap=args[0] is "bootstrap" or "ci-bootstrap";
    if (bootstrap && LocalDatabase.IsCi != (args[0]=="ci-bootstrap"))
        throw new InvalidOperationException("Use the explicit bootstrap command for this environment.");
    var connectionString=args[0]=="apply" ? LocalDatabase.RoleConnectionString("qr_migrator") : LocalDatabase.ConnectionString();
    await using var context=LocalDatabase.CreateContext(connectionString);
    if (args[0] is "script" or "rls-script" or "mapping-script")
        Console.Write(context.GetService<IMigrator>().GenerateScript(
            args[0]=="mapping-script" ? "20261026040035_TenantRowSecurity" : args[0]=="rls-script" ? "20261026033754_InitialFoundation" : null,
            args[0]=="rls-script" ? "20261026040035_TenantRowSecurity" : null));
    else
    {
        if (!LocalDatabase.IsCi)
            throw new InvalidOperationException("Identity mapping migration is restricted to an explicitly configured disposable CI database pending separate development-database review.");
        if (args[0]=="ci-bootstrap")
        {
            await context.Database.OpenConnectionAsync();
            await using var check=context.Database.GetDbConnection().CreateCommand();
            check.CommandText="SELECT count(*) FROM pg_namespace WHERE nspname IN ('identity','organizations','foundation')";
            if (Convert.ToInt64(await check.ExecuteScalarAsync()) != 0)
                throw new InvalidOperationException("CI bootstrap requires an empty disposable database.");
        }
        if (!bootstrap)
        {
            await context.Database.OpenConnectionAsync();
            await context.Database.ExecuteSqlRawAsync("SET ROLE qr_platform_owner");
        }
        await context.Database.MigrateAsync();
        if (bootstrap) await LocalDatabase.ProvisionLocalRolePasswordsAsync();
        Console.WriteLine("Explicit synthetic database setup completed. Credentials were not displayed.");
    }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Local setup failed ({error.GetType().Name}); no credentials or SQL parameters displayed.");
    return 1;
}
