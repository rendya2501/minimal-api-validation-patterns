using Carter;
using FluentValidation;
using MinimalApiValidationPatterns.Behaviors;
using MinimalApiValidationPatterns.Data;
using MinimalApiValidationPatterns.ExceptionHandling;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// サービスコンテナの設定
/// </summary>
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCarter();

// MediatRの設定
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    // パイプラインの順序が重要
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation: バリデーション定義ライブラリ
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// グローバル例外ハンドラー
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// インメモリデータベース (シングルトン)
builder.Services.AddSingleton<InMemoryDatabase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// 例外ハンドラーを有効化
app.UseExceptionHandler();

// Carterのエンドポイントをマッピング
app.MapCarter();

app.Run();

// 古い例外ハンドラーの例
//app.UseExceptionHandler(exceptionHandlerApp =>
//{
//    exceptionHandlerApp.Run(async context =>
//    {
//        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

//        if (exception is null)
//            return;

//        var problemDetailsFactory =
//            context.RequestServices.GetRequiredService<ProblemDetailsFactory>();

//        if (exception is FluentValidation.ValidationException ve)
//        {
//            var modelState = new ModelStateDictionary();

//            foreach (var error in ve.Errors)
//            {
//                modelState.AddModelError(
//                    error.PropertyName,
//                    error.ErrorMessage);
//            }

//            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
//                context,
//                modelState,
//                statusCode: StatusCodes.Status400BadRequest
//            );

//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            context.Response.ContentType = "application/problem+json";

//            await context.Response.WriteAsJsonAsync(problemDetails);
//            return;
//        }

//        // その他の例外
//        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
//    });
//});
