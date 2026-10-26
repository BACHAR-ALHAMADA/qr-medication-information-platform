using DatabaseSetup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
try
{
    if (args.Length != 1 || args[0] is not ("script" or "rls-script" or "apply" or "bootstrap"))
        throw new ArgumentException("Use script, rls-script, bootstrap or apply. Local development only.");
    var bootstrap=args[0]=="bootstrap";
    var connectionString=args[0]=="apply" ? LocalDatabase.RoleConnectionString("qr_migrator") : LocalDatabase.ConnectionString();
    await using var context=LocalDatabase.CreateContext(connectionString);
    if (args[0] is "script" or "rls-script")
        Console.Write(context.GetService<IMigrator>().GenerateScript(args[0]=="rls-script" ? "20261026033754_InitialFoundation" : null));
    else
    {
        if (!bootstrap)
        {
            await context.Database.OpenConnectionAsync();
            await context.Database.ExecuteSqlRawAsync("SET ROLE qr_platform_owner");
        }
        await context.Database.MigrateAsync();
        if (bootstrap) await LocalDatabase.ProvisionLocalRolePasswordsAsync();
        Console.WriteLine("Local development migrations applied. Credentials were not displayed.");
    }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Local setup failed ({error.GetType().Name}); no credentials or SQL parameters displayed.");
    return 1;
}