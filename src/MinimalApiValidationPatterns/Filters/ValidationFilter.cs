using FluentValidation;

namespace MinimalApiValidationPatterns.Filters;

/// <summary>
/// フィルターを実行
/// </summary>
/// <param name="context">エンドポイントフィルター呼び出しコンテキスト</param>
/// <param name="next">次のフィルターまたはエンドポイント</param>
/// <returns>
/// バリデーション成功時は次の処理の結果、
/// 失敗時は ValidationProblem レスポンス
/// </returns>
public class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    /// <summary>
    /// フィルターを実行
    /// </summary>
    /// <param name="context">エンドポイントフィルター呼び出しコンテキスト</param>
    /// <param name="next">次のフィルターまたはエンドポイント</param>
    /// <returns>
    /// バリデーション成功時は次の処理の結果、
    /// 失敗時は ValidationProblem レスポンス
    /// </returns>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // リクエストオブジェクトを引数から取得
        var request = context.Arguments.OfType<TRequest>().First();

        // バリデーション実行
        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            // エラーをプロパティ名でグループ化
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            // RFC 9110 準拠の Problem Details を返す
            return TypedResults.ValidationProblem(errors);
        }

        // バリデーション成功 - 次の処理へ
        return await next(context);
    }
}

/// <summary>
/// ValidationFilter を簡単に適用するための拡張メソッド
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// エンドポイントにリクエストバリデーションを追加
    /// </summary>
    /// <typeparam name="TRequest">バリデーション対象のリクエスト型</typeparam>
    /// <param name="builder">ルートハンドラービルダー</param>
    /// <returns>フィルターが追加されたビルダー</returns>
    /// <example>
    /// <code>
    /// endpoints.MapPost("/posts", CreatePost)
    ///     .WithRequestValidation&lt;CreatePostRequest&gt;();
    /// </code>
    /// </example>
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem();
    }
}
