namespace ImageViewer.Core.Models;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly ViewerError? _error;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(ViewerError error)
    {
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public ViewerError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access the error of a successful result.");

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public static Result<T> Failure(ViewerError error)
    {
        return new Result<T>(error);
    }
}

public sealed class Result
{
    private readonly ViewerError? _error;

    private Result()
    {
        IsSuccess = true;
    }

    private Result(ViewerError error)
    {
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ViewerError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access the error of a successful result.");

    public static Result Success()
    {
        return new Result();
    }

    public static Result Failure(ViewerError error)
    {
        return new Result(error);
    }
}
