using System.Data;
using Npgsql;

namespace Identity.Infrastructure;

/// <summary>
/// Privileged disposable-test boundary only. Never an application authentication bootstrap.
/// It intentionally rejects all existing runtime/migration/setup roles and the development database.
/// </summary>
public sealed class SyntheticIdentityMappingSession : IAsyncDisposable
{
    public NpgsqlConnection Connection { get; }
    public NpgsqlTransaction Transaction { get; }
    private bool disposed;
    internal bool IsActive => !disposed;
    private SyntheticIdentityMappingSession(NpgsqlConnection connection, NpgsqlTransaction transaction)
        => (Connection, Transaction) = (connection, transaction);

    public static async Task<SyntheticIdentityMappingSession> OpenAsync(NpgsqlDataSource source,
        CancellationToken cancellationToken = default)
    {
        var connection = await source.OpenConnectionAsync(cancellationToken);
        SyntheticIdentityMappingSession? session = null;
        try
        {
            var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
            session = new(connection, transaction);
            await using var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction);
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
            await using var command = new NpgsqlCommand("""
                SELECT current_database()='qr_medication_ci' AND session_user='qr_ci_admin'
                    AND current_user=session_user AND (SELECT rolsuper FROM pg_roles WHERE rolname=current_user)
                """, connection, transaction);
            // Named synthetic administrator + disposable database are deliberate, not production security.
            if (!Equals(await command.ExecuteScalarAsync(cancellationToken), true))
                throw new InvalidOperationException("Identity mapping requires the privileged disposable synthetic harness.");
            return session;
        }
        catch
        {
            if (session is not null) await session.DisposeAsync(); else await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { await Transaction.RollbackAsync(cleanup.Token); }
            catch { NpgsqlConnection.ClearPool(Connection); throw; }
        }
        finally { try { await Transaction.DisposeAsync(); } finally { await Connection.DisposeAsync(); } }
    }
}
