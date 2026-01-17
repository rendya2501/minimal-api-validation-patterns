using FluentValidation;
using MediatR;

namespace MinimalApiValidationPatterns.Behaviors;

/// <summary>
/// MediatR の Pipeline Behavior。
/// Handler が呼ばれる前に FluentValidation を実行するための共通処理。
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
/// <param name="validators"></param>
/// <remarks>
/// DI から「TRequest に対応する Validator」をすべて受け取る
/// 通常は 0 個 or 1 個だが、複数あっても動く設計
/// </remarks>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    // MediatR のパイプラインに割り込むためのインターフェース
    : IPipelineBehavior<TRequest, TResponse>
    // TRequest は null 不可（ValidationContext が null を想定していないため）
    where TRequest : notnull
{
    /// <summary>
    /// MediatR が Request を処理するたびに必ず呼ばれるメソッド
    /// </summary>
    /// <param name="request">実際に送信された Command / Query</param>
    /// <param name="next">次の処理（次の Behavior or 最終的な Handler）</param>
    /// <param name="ct">キャンセル用トークン（ほぼ素通し）</param>
    /// <returns></returns>
    /// <exception cref="ValidationException"></exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
        {
            return await next(ct);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct))
        );

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next(ct);
    }
}
