using System.Text.Json.Serialization;

namespace History.Commons;

[JsonConverter(typeof(JsonStringEnumConverter<ErrorType>))]
public enum ErrorType
{
    NotFound,
    Forbidden,
    Conflict,
    BadRequest,
    Unauthorized,
    ProgramError
}

public class Result
{
    public ErrorType? Error { get; protected init; }
    public string ErrorMessage { get; protected init; }
    protected string TypeName { get; set; }
    public string FullErrorMessage => Error.HasValue ? $"{(string.IsNullOrEmpty(TypeName) ? string.Empty : $"[{TypeName}] ")}{Error}: {ErrorMessage ?? "N/A"}" : null;
    public bool IsSuccess => !Error.HasValue;
    public bool IsFailure => Error.HasValue;

    protected Result(ErrorType? error = null, string errorMessage = null)
    {
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static implicit operator Result((ErrorType, string) input) => Failure(input.Item1, input.Item2);

    public static Result Success() => new(null, null);
    public static Result Failure(ErrorType error, string errorMessage = null) => new(error, errorMessage);
}

public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value, ErrorType? error, string errorMessage) : base(error, errorMessage)
    {
        TypeName = typeof(T).Name;
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>((ErrorType, string) input) => Failure(input.Item1, input.Item2);
    public static implicit operator T(Result<T> result) => result.Error.HasValue ? throw new InvalidOperationException($"Cannot retrieve value due to error: {result.Error}/{result.ErrorMessage}") : result.Value;

    public static Result<T> Success(T value) => new(value, null, null);
    public static Result<T> Failure(ErrorType error, string errorMessage = null, T value = default) => new(value, error, errorMessage);
    public static Result<T> Failure(Result result, T value = default) => new(value, result.Error, result.ErrorMessage);
}

public static class ResultExtensions
{
    public static Result<T> CastFailure<T>(this Result result)
    {
        if (result.IsSuccess) throw new InvalidOperationException("Cannot cast a successful result as failure.");
        return Result<T>.Failure(result.Error.Value, result.ErrorMessage, default);
    }

    public static Result CastFailure<T>(this Result<T> result)
    {
        if (result.IsSuccess) throw new InvalidOperationException("Cannot cast a successful result as failure.");
        return Result<T>.Failure(result.Error.Value, result.ErrorMessage, default);
    }
}
