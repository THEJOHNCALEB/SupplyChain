namespace SupplyChain.Domain.Common;

/// <summary>
/// A lightweight result type used for expected, user-facing failures instead of
/// throwing exceptions across service boundaries.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string Error { get; init; } = string.Empty;

    public static ServiceResult Ok() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public static new ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}
