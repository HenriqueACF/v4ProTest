namespace BksMarine.Application.Common;

public sealed record Error(string Code, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;

    private Result(Error error) : base(false, error) => Value = default;

    public static Result<T> Ok(T value) => new(value);
    public static new Result<T> Fail(Error error) => new(error);
}
