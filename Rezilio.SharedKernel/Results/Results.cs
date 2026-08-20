namespace Rezilio.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }
    public string? Error => Errors.FirstOrDefault();

    public static Result Success() => new(true, []);
    public static Result Failure(string error) => new(false, [error]);
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(value, true, []);
    public static Result<T> Failure<T>(string error) => new(default, false, [error]);
    public static Result<T> Failure<T>(IReadOnlyList<string> errors) => new(default, false, errors);
}

public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");
}
