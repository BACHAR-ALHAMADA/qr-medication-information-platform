using System.Data;
using Npgsql;
namespace PostgreSql.Infrastructure;

/// <summary>Issued only by an explicitly trusted synthetic harness. This is not an authenticated principal.</summary>
public sealed class TrustedSyntheticActor
{
    public Guid Id { get; }
    private TrustedSyntheticActor(Guid id) { Id=id; }
    public static TrustedSyntheticActor FromTrustedHarness(Guid id)
        => id == Guid.Empty ? throw new ArgumentException("Actor reference required.") : new(id);
}

/// <summary>Technical transaction boundary only; it neither authenticates nor authorizes an actor.</summary>
public sealed class ReadOnlyTenantSession : IAsyncDisposable
{
    public NpgsqlConnection Connection { get; }
    public NpgsqlTransaction Transaction { get; }
    private bool completed;
    private bool disposed;
    private ReadOnlyTenantSession(NpgsqlConnection connection,NpgsqlTransaction transaction)
        { Connection=connection;Transaction=transaction; }

    public static async Task<ReadOnlyTenantSession> OpenAsync(NpgsqlDataSource dataSource,TrustedSyntheticActor actor,
        Guid candidateOrganization,Guid candidateBranch,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (candidateOrganization==Guid.Empty || candidateBranch==Guid.Empty) throw new ArgumentException("Explicit candidate scope required.");
        var connection=await dataSource.OpenConnectionAsync(cancellationToken);
        ReadOnlyTenantSession? scope=null;
        try
        {
            var transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead,cancellationToken);
            scope=new(connection,transaction);
            await using var readOnly=new NpgsqlCommand("SET TRANSACTION READ ONLY",connection,transaction);
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
            // Reject accidental administrative/migration datasource wiring in this local foundation.
            await using var identity=new NpgsqlCommand("SELECT session_user='qr_reader' AND current_user=session_user",connection,transaction);
            if (!Equals(await identity.ExecuteScalarAsync(cancellationToken),true))
                throw new InvalidOperationException("Tenant sessions require the dedicated reader credentials.");
            await using var command=new NpgsqlCommand("""
                SELECT set_config('app.actor_id',$1,true),set_config('app.organization_id',$2,true),set_config('app.branch_id',$3,true);
                """,connection,transaction);
            command.Parameters.AddWithValue(actor.Id.ToString("D"));
            command.Parameters.AddWithValue(candidateOrganization.ToString("D"));
            command.Parameters.AddWithValue(candidateBranch.ToString("D"));
            await command.ExecuteNonQueryAsync(cancellationToken);
            return scope;
        }
        catch
        {
            if (scope != null) await scope.DisposeAsync(); else await connection.DisposeAsync();
            throw;
        }
    }
    public async Task CommitAsync(CancellationToken cancellationToken=default)
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        await Transaction.CommitAsync(cancellationToken);
        completed=true;
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed=true;
        try
        {
            if (!completed)
            {
                using var cleanup=new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await Transaction.RollbackAsync(cleanup.Token); }
                catch { NpgsqlConnection.ClearPool(Connection); throw; }
            }
        }
        finally { try { await Transaction.DisposeAsync(); } finally { await Connection.DisposeAsync(); } }
    }
}