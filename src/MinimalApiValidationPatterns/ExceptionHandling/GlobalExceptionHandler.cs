using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;

namespace MinimalApiValidationPatterns.ExceptionHandling;

/// <summary>
/// グローバル例外ハンドラー
/// </summary>
/// <remarks>
/// ASP.NET Core 8+ の IExceptionHandler パターンを使用した実装
/// </remarks>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorContext = MapExceptionToErrorContext(exception);

        logger.LogError(
            exception,
            "Unhandled exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message
        );

        // レスポンスがすでに開始されている場合は処理しない
        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning("Response has already started, cannot handle exception");
            return false;
        }

        var problemDetails = CreateProblemDetails(
            httpContext,
            errorContext,
            exception
        );
   
        httpContext.Response.StatusCode = errorContext.StatusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problemDetails,
            cancellationToken: cancellationToken
        );

        return true;
    }

    /// <summary>
    /// 例外をエラーコンテキストにマッピング
    /// </summary>
    private ErrorContext MapExceptionToErrorContext(Exception exception)
    {
        return exception switch
        {
            // FluentValidation のバリデーションエラー
            ValidationException validationEx => new ErrorContext(
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),

            // カスタム例外: リソースが見つからない
            NotFoundException notFoundEx => new ErrorContext(
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFoundEx.Message
            ),

            // 認証エラー
            UnauthorizedAccessException => new ErrorContext(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource."
            ),

            // 引数エラー
            ArgumentException argumentEx => new ErrorContext(
                StatusCodes.Status400BadRequest,
                "Invalid Argument",
                environment.IsDevelopment()
                    ? argumentEx.Message
                    : "The request contains invalid arguments."
            ),

            // 無効な操作
            InvalidOperationException invalidOpEx => new ErrorContext(
                StatusCodes.Status400BadRequest,
                "Invalid Operation",
                environment.IsDevelopment()
                    ? invalidOpEx.Message
                    : "The requested operation is not valid in the current state."
            ),

            // リクエストキャンセル（499 Client Closed Request）
            OperationCanceledException => new ErrorContext(
                StatusCodes.Status499ClientClosedRequest,
                "Request Cancelled",
                "The request was cancelled by the client.",
                SuppressBody: true
            ),

            // データベース関連エラー（EF Coreを使っている場合）
            // DbUpdateException dbUpdateEx => new ErrorContext(
            //     StatusCodes.Status409Conflict,
            //     "Database Conflict",
            //     environment.IsDevelopment() 
            //         ? dbUpdateEx.Message 
            //         : "A database conflict occurred."
            // ),

            // その他すべての例外
            _ => new ErrorContext(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
            )
        };
    }

    /// <summary>
    /// ProblemDetails オブジェクトを生成
    /// </summary>
    private ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        ErrorContext errorContext,
        Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = errorContext.StatusCode,
            Title = errorContext.Title,
            Detail = errorContext.Detail,
            Type = GetProblemDetailsType(errorContext.StatusCode),
            Instance = httpContext.Request.Path
        };

        // バリデーションエラーの詳細を追加
        if (errorContext.ValidationErrors is not null)
        {
            problemDetails.Extensions["errors"] = errorContext.ValidationErrors;
        }

        // 開発環境でのみスタックトレースを含める
        if (environment.IsDevelopment() && !errorContext.SuppressBody)
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;

            if (exception.InnerException is not null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    Type = exception.InnerException.GetType().Name,
                    Message = exception.InnerException.Message
                };
            }
        }

        // TraceIdを追加（分散トレーシング対応）
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problemDetails;
    }

    /// <summary>
    /// RFC 9110準拠のProblem Details Type URIを取得
    /// </summary>
    private static string GetProblemDetailsType(int statusCode)
    {
        var section = statusCode switch
        {
            >= 400 and < 500 => "15.5",
            >= 500 and < 600 => "15.6",
            _ => "15"
        };

        var subsection = statusCode switch
        {
            StatusCodes.Status400BadRequest => "1",
            StatusCodes.Status401Unauthorized => "2",
            StatusCodes.Status403Forbidden => "4",
            StatusCodes.Status404NotFound => "5",
            StatusCodes.Status405MethodNotAllowed => "6",
            StatusCodes.Status409Conflict => "10",
            StatusCodes.Status500InternalServerError => "1",
            StatusCodes.Status501NotImplemented => "2",
            StatusCodes.Status503ServiceUnavailable => "4",
            _ => "1"
        };

        return $"https://tools.ietf.org/html/rfc9110#section-{section}.{subsection}";
    }
}
