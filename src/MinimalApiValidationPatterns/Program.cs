using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RequestValidationInMinimalAPIs.Behaviors;
using RequestValidationInMinimalAPIs.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddCarter();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<InMemoryDatabase>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapCarter();

//app.UseExceptionHandler(exceptionHandlerApp =>
//{
//    exceptionHandlerApp.Run(async context =>
//    {
//        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

//        if (exception is ValidationException validationException)
//        {
//            context.Response.StatusCode = StatusCodes.Status400BadRequest;
//            context.Response.ContentType = "application/problem+json";

//            var problem = new ValidationProblemDetails(
//                validationException.Errors
//                    .GroupBy(e => e.PropertyName)
//                    .ToDictionary(
//                        g => g.Key,
//                        g => g.Select(e => e.ErrorMessage).ToArray()
//                    )
//            );
//            var errors = validationException.Errors
//               .GroupBy(e => e.PropertyName)
//               .ToDictionary(
//                   g => g.Key,
//                   g => g.Select(e => e.ErrorMessage).ToArray()
//               );
//            var validationProblem = TypedResults.ValidationProblem(errors);
//            await context.Response.WriteAsJsonAsync(validationProblem.ProblemDetails);
//            // await context.Response.WriteAsJsonAsync(problem);
//            return;
//        }

//        // ‚»‚Ì‘¼‚Ì—áŠO
//        context.Response.StatusCode = 500;
//    });
//});
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is null) 
            return;

        var problemDetailsFactory =
            context.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        if (exception is ValidationException ve)
        {
            var modelState = new ModelStateDictionary();

            foreach (var error in ve.Errors)
            {
                modelState.AddModelError(
                    error.PropertyName,
                    error.ErrorMessage);
            }

            var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                context,
                modelState,
                statusCode: StatusCodes.Status400BadRequest
            );

            context.Response.StatusCode =StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        // ‚»‚Ì‘¼‚Ì—áŠO
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});


app.Run();
