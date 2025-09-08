namespace AccountService;
public class Result<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Type { get; init; }
    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new Result<T> { Success = true, Data = data };
    public static Result<T> Fail(string message, string type) => new Result<T> { Success = false, Message = message, Type = type };
}
