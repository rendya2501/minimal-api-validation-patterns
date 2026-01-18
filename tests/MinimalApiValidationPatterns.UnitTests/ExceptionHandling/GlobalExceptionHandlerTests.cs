using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalApiValidationPatterns.ExceptionHandling;
using System.Net;

namespace MinimalApiValidationPatterns.UnitTests.ExceptionHandling;

/// <summary>
/// GlobalExceptionHandler のテスト
/// </summary>
/// <remarks>
/// グローバル例外ハンドラーが様々な例外を適切な HTTP ステータスコードと
/// Problem Details 形式に変換することを検証します。
/// </remarks>
public class GlobalExceptionHandlerTests
{
    /// <summary>
    /// ValidationException が 400 Bad Request として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_ValidationException_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            var errors = new[]
            {
                new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
                new FluentValidation.Results.ValidationFailure("Age", "Age must be positive")
            };
            throw new ValidationException(errors);
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Error");
        content.Should().Contain("Name");
        content.Should().Contain("Age");
    }

    /// <summary>
    /// NotFoundException が 404 Not Found として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_NotFoundException_ShouldReturn404()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new NotFoundException("Post", Guid.NewGuid());
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Resource Not Found");
        content.Should().Contain("Post");
    }

    /// <summary>
    /// UnauthorizedAccessException が 401 Unauthorized として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_UnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new UnauthorizedAccessException();
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unauthorized");
    }

    /// <summary>
    /// ArgumentException が 400 Bad Request として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_ArgumentException_ShouldReturn400()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new ArgumentException("Invalid argument provided");
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid Argument");
    }

    /// <summary>
    /// InvalidOperationException が 400 Bad Request として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_InvalidOperationException_ShouldReturn400()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new InvalidOperationException("Invalid operation");
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid Operation");
    }

    /// <summary>
    /// 一般的な例外が 500 Internal Server Error として処理されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_GeneralException_ShouldReturn500()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new Exception("Unexpected error");
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Internal Server Error");
    }

    /// <summary>
    /// Problem Details に traceId が含まれることを検証
    /// </summary>
    [Fact]
    public async Task Handle_ShouldIncludeTraceId()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            throw new Exception("Test exception");
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("traceId");
    }

    /// <summary>
    /// 開発環境で例外の詳細情報が含まれることを検証
    /// </summary>
    [Fact]
    public async Task Handle_InDevelopment_ShouldIncludeExceptionDetails()
    {
        // Arrange
        await using var factory = CreateTestFactory(
            ctx => throw new Exception("Test exception"),
            isDevelopment: true
        );
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("exception");
        content.Should().Contain("stackTrace");
    }

    /// <summary>
    /// 本番環境で例外の詳細情報が含まれないことを検証
    /// </summary>
    [Fact]
    public async Task Handle_InProduction_ShouldNotIncludeExceptionDetails()
    {
        // Arrange
        await using var factory = CreateTestFactory(
            ctx => throw new Exception("Test exception with sensitive data"),
            isDevelopment: false
        );
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("stackTrace");
        content.Should().NotContain("sensitive data");
    }

    /// <summary>
    /// ValidationException のエラーが errors フィールドにグループ化されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_ValidationException_ShouldGroupErrorsByProperty()
    {
        // Arrange
        await using var factory = CreateTestFactory(ctx =>
        {
            var errors = new[]
            {
                new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
                new FluentValidation.Results.ValidationFailure("Name", "Name must be at least 3 characters"),
                new FluentValidation.Results.ValidationFailure("Age", "Age must be positive")
            };
            throw new ValidationException(errors);
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/test");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"errors\"");

        // Name プロパティに2つのエラーがあることを確認
        var nameErrorCount = System.Text.RegularExpressions.Regex.Matches(content, "Name").Count;
        nameErrorCount.Should().BeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// テスト用の WebApplicationFactory を作成するヘルパーメソッド
    /// </summary>
    private WebApplicationFactory<Program> CreateTestFactory(
        Action<HttpContext> throwException,
        bool isDevelopment = true)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(isDevelopment ? "Development" : "Production");
                builder.ConfigureTestServices(services =>
                {
                    // GlobalExceptionHandler は既に登録されているので、
                    // テスト用のエンドポイントのみ追加
                });
                builder.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", (HttpContext ctx) =>
                        {
                            throwException(ctx);
                            return Results.Ok();
                        });
                    });
                });
            });
    }
}