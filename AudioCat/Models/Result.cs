// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace AudioCat.Models;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string Message { get; }
}

public interface IResultCode<out T> : IResult where T : struct
{
    T Code { get; }
}

public interface IResponse<out T> : IResult
{
    T? Data { get; }
}

public interface IResponseWithCode<out TCodeType, out TResponseType> : IResultCode<TCodeType>, IResponse<TResponseType> where TCodeType : struct;

public class Result : IResult
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure { get; private init; }
    public string Message { get; private init; } = "";
    private Result() { }
    public static IResult Success() { return new Result { IsSuccess = true }; }
    public static IResult Success(string message) { return new Result { IsSuccess = true, Message = message }; }
    public static IResult Failure() { return new Result { IsFailure = true }; }
    public static IResult Failure(string message) { return new Result { IsFailure = true, Message = message }; }
}

public class ResultCode<T> : IResultCode<T> where T : struct
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure { get; private init; }
    public T Code { get; private init; }
    public string Message { get; private init; } = "";
    private ResultCode() { }
    public static IResultCode<T> Success() { return new ResultCode<T> { IsSuccess = true }; }
    public static IResultCode<T> Success(T code) { return new ResultCode<T> { IsSuccess = true, Code = code }; }
    public static IResultCode<T> Success(T code, string message) { return new ResultCode<T> { IsSuccess = true, Code = code, Message = message }; }
    public static IResultCode<T> Failure() { return new ResultCode<T> { IsFailure = true }; }
    public static IResultCode<T> Failure(T code) { return new ResultCode<T> { IsFailure = true, Code = code }; }
    public static IResultCode<T> Failure(T code, string message) { return new ResultCode<T> { IsFailure = true, Code = code, Message = message }; }
}

public class ResponseWithCode<TCode, TData> : IResponseWithCode<TCode, TData> where TCode : struct
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure { get; private init; }
    public string Message { get; private init; } = "";
    public TCode Code { get; private init; }
    public TData? Data { get; private init; }
    private ResponseWithCode() { }
    public static IResponseWithCode<TCode, TData> Success() { return new ResponseWithCode<TCode, TData> { IsSuccess = true }; }
    public static IResponseWithCode<TCode, TData> Success(TData data, string message = "") { return new ResponseWithCode<TCode, TData> { IsSuccess = true, Data = data, Message = message }; }
    public static IResponseWithCode<TCode, TData> Success(TCode code, TData data, string message = "") { return new ResponseWithCode<TCode, TData> { IsSuccess = true, Code = code, Data = data, Message = message }; }
    public static IResponseWithCode<TCode, TData> Failure(TCode code, string message = "") { return new ResponseWithCode<TCode, TData> { IsFailure = true, Code = code, Message = message }; }
    public static IResponseWithCode<TCode, TData> Failure(TCode code, TData data, string message = "") { return new ResponseWithCode<TCode, TData> { IsFailure = true, Code = code, Data = data, Message = message }; }
}

public class Response<T> : IResponse<T>
{
    public bool IsSuccess { get; private init; }
    public bool IsFailure { get; private init; }
    public string Message { get; private init; } = "";
    public T? Data { get; private init; }
    private Response() { }
    public static IResponse<T> Success() { return new Response<T> { IsSuccess = true }; }
    public static IResponse<T> Success(T data) { return new Response<T> { IsSuccess = true, Data = data }; }
    public static IResponse<T> Success(T data, string message) { return new Response<T> { IsSuccess = true, Data = data, Message = message }; }
    public static IResponse<T> Failure() { return new Response<T> { IsFailure = true }; }
    public static IResponse<T> Failure(string message) { return new Response<T> { IsFailure = true, Message = message }; }
    public static IResponse<T> Failure(IResult result) { return new Response<T> { IsFailure = true, Message = result.Message }; }
    public static IResponse<T> Failure(T data) { return new Response<T> { IsFailure = true, Data = data }; }
    public static IResponse<T> Failure(T data, string message) { return new Response<T> { IsFailure = true, Data = data, Message = message }; }
}