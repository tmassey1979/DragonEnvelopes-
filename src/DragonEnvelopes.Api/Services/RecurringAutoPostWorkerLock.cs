using System.Data;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Api.Services;

public sealed class RecurringAutoPostWorkerLock(
    DragonEnvelopesDbContext dbContext,
    IOptions<RecurringAutoPostWorkerOptions> optionsAccessor,
    ILogger<RecurringAutoPostWorkerLock> logger) : IRecurringAutoPostWorkerLock
{
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private static readonly SemaphoreSlim InProcessLock = new(initialCount: 1, maxCount: 1);

    public async Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        var options = optionsAccessor.Value;
        if (!options.UseDistributedLock)
        {
            return await TryAcquireInProcessAsync(cancellationToken);
        }

        if (!string.Equals(dbContext.Database.ProviderName, NpgsqlProviderName, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Recurring auto-post worker lock fell back to in-process mode because provider '{ProviderName}' is not PostgreSQL.",
                dbContext.Database.ProviderName ?? "<null>");
            return await TryAcquireInProcessAsync(cancellationToken);
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();
        await using var lockCommand = connection.CreateCommand();
        lockCommand.CommandText = "SELECT pg_try_advisory_lock(@lock_key);";
        var lockParameter = lockCommand.CreateParameter();
        lockParameter.ParameterName = "@lock_key";
        lockParameter.DbType = DbType.Int64;
        lockParameter.Value = options.DistributedLockKey;
        lockCommand.Parameters.Add(lockParameter);

        var result = await lockCommand.ExecuteScalarAsync(cancellationToken);
        if (result is not bool acquired || !acquired)
        {
            await dbContext.Database.CloseConnectionAsync();
            return null;
        }

        return new PostgresLockLease(dbContext, options.DistributedLockKey);
    }

    private static async Task<IAsyncDisposable?> TryAcquireInProcessAsync(CancellationToken cancellationToken)
    {
        if (!await InProcessLock.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            return null;
        }

        return new InProcessLockLease();
    }

    private sealed class InProcessLockLease : IAsyncDisposable
    {
        private bool _released;

        public ValueTask DisposeAsync()
        {
            if (_released)
            {
                return ValueTask.CompletedTask;
            }

            _released = true;
            InProcessLock.Release();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PostgresLockLease(
        DragonEnvelopesDbContext dbContext,
        long lockKey) : IAsyncDisposable
    {
        private bool _released;

        public async ValueTask DisposeAsync()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            try
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await dbContext.Database.OpenConnectionAsync();
                }

                await using var unlockCommand = connection.CreateCommand();
                unlockCommand.CommandText = "SELECT pg_advisory_unlock(@lock_key);";
                var unlockParameter = unlockCommand.CreateParameter();
                unlockParameter.ParameterName = "@lock_key";
                unlockParameter.DbType = DbType.Int64;
                unlockParameter.Value = lockKey;
                unlockCommand.Parameters.Add(unlockParameter);
                await unlockCommand.ExecuteScalarAsync();
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
