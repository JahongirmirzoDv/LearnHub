using Microsoft.EntityFrameworkCore;

namespace LearnHub.Data;

public static class TransactionExtensions
{
    /// <summary>
    /// Runs several database commands atomically. The transaction is wrapped in the execution strategy so the
    /// method stays correct if a retrying strategy is ever configured.
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
