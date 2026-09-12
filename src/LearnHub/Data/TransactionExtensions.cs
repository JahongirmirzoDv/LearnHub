using Microsoft.EntityFrameworkCore;

namespace LearnHub.Data;

public static class TransactionExtensions
{
    /// <summary>
    /// Runs several database commands atomically. Wrapping the transaction in the execution strategy is
    /// required when SQL Server retry-on-failure is enabled, and harmless for SQLite.
    /// </summary>
    public static Task InTransactionAsync(this ApplicationDbContext db, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await operation();
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
