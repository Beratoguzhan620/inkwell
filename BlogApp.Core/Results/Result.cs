namespace BlogApp.Core.Results;

public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Result Ok(string message = "")
    {
        return new Result { Success = true, Message = message };
    }

    public static Result Fail(string message)
    {
        return new Result { Success = false, Message = message };
    }
}


public class Result<T> : Result
{
    public T? Data { get; set; } = default!;

    public static Result<T> Ok(T data, string message = "")
    {
        return new Result<T> { Success = true, Data = data, Message = message };
    }

    public static Result<T> Fail(string message, T? data = default)
    {
        return new Result<T> { Success = false, Data = data, Message = message };
    }
}


