namespace LearnHub.Services;

public enum OperationStatus
{
    Succeeded,
    NotFound,
    Failed
}

/// <summary>
/// Outcome of a service operation that can fail for a business reason (e.g. "category still has courses").
/// Controllers translate it into a redirect, a 404 or a validation message.
/// </summary>
public class OperationResult
{
    protected OperationResult(OperationStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public OperationStatus Status { get; }

    public string? Error { get; }

    public bool Succeeded => Status == OperationStatus.Succeeded;

    public bool IsNotFound => Status == OperationStatus.NotFound;

    public static OperationResult Success() => new(OperationStatus.Succeeded, null);

    public static OperationResult NotFound() => new(OperationStatus.NotFound, null);

    public static OperationResult Failure(string error) => new(OperationStatus.Failed, error);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(OperationStatus status, T? value, string? error)
        : base(status, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(OperationStatus.Succeeded, value, null);

    public static new OperationResult<T> NotFound() => new(OperationStatus.NotFound, default, null);

    public static new OperationResult<T> Failure(string error) => new(OperationStatus.Failed, default, error);
}
