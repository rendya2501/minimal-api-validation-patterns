using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MinimalApiValidationPatterns.Tests.Shared.Extensions;

/// <summary>
/// HttpResponseMessage の拡張メソッド
/// </summary>
/// <remarks>
/// 統合テストで頻繁に使用するアサーションやヘルパーメソッドを提供します。
/// </remarks>
public static class HttpResponseExtensions
{
    /// <summary>
    /// レスポンスが成功ステータスコードであることを検証し、
    /// JSON を指定した型にデシリアライズ
    /// </summary>
    public static async Task<T> ShouldBeSuccessWithContent<T>(this HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success status code but got {response.StatusCode}");

        var content = await response.Content.ReadFromJsonAsync<T>();
        content.Should().NotBeNull();
        return content!;
    }

    /// <summary>
    /// レスポンスが指定されたステータスコードであることを検証
    /// </summary>
    public static HttpResponseMessage ShouldHaveStatusCode(
        this HttpResponseMessage response,
        int expectedStatusCode)
    {
        ((int)response.StatusCode).Should().Be(expectedStatusCode,
            $"Expected status code {expectedStatusCode} but got {response.StatusCode}");
        return response;
    }

    /// <summary>
    /// レスポンスが Problem Details 形式であることを検証
    /// </summary>
    public static async Task<ProblemDetailsResponse> ShouldBeProblemDetails(
        this HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        problemDetails.Should().NotBeNull();
        return problemDetails!;
    }

    /// <summary>
    /// レスポンスがバリデーションエラー（400 Bad Request）を含むことを検証
    /// </summary>
    /// <remarks>
    /// Filter パターンと Pipeline パターンでタイトルが異なるため、
    /// タイトルの検証は行わず、ステータスコードとエラーの存在のみを検証します。
    /// 
    /// - Filter: TypedResults.ValidationProblem → "One or more validation errors occurred."
    /// - Pipeline: GlobalExceptionHandler → "Validation Error"
    /// </remarks>
    public static async Task<Dictionary<string, string[]>> ShouldHaveValidationErrors(
        this HttpResponseMessage response,
        params string[] expectedPropertyNames)
    {
        var problemDetails = await response.ShouldBeProblemDetails();

        // ステータスコードは必ず 400
        problemDetails.Status.Should().Be(400);

        // エラーディクショナリが存在することを確認
        problemDetails.Errors.Should().NotBeNull();

        // 期待されるプロパティ名のエラーが含まれることを確認
        foreach (var propertyName in expectedPropertyNames)
        {
            problemDetails.Errors.Should().ContainKey(propertyName,
                $"Expected validation error for property '{propertyName}'");
        }

        return problemDetails.Errors!;
    }

    /// <summary>
    /// レスポンスが Filter パターンのバリデーションエラーであることを検証
    /// </summary>
    /// <remarks>
    /// TypedResults.ValidationProblem によって生成される Problem Details を検証します。
    /// </remarks>
    public static async Task<Dictionary<string, string[]>> ShouldHaveFilterValidationErrors(
        this HttpResponseMessage response,
        params string[] expectedPropertyNames)
    {
        var problemDetails = await response.ShouldBeProblemDetails();

        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("One or more validation errors occurred.");
        problemDetails.Errors.Should().NotBeNull();

        foreach (var propertyName in expectedPropertyNames)
        {
            problemDetails.Errors.Should().ContainKey(propertyName);
        }

        return problemDetails.Errors!;
    }

    /// <summary>
    /// レスポンスが Pipeline パターンのバリデーションエラーであることを検証
    /// </summary>
    /// <remarks>
    /// GlobalExceptionHandler によって生成される Problem Details を検証します。
    /// </remarks>
    public static async Task<Dictionary<string, string[]>> ShouldHavePipelineValidationErrors(
        this HttpResponseMessage response,
        params string[] expectedPropertyNames)
    {
        var problemDetails = await response.ShouldBeProblemDetails();

        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Validation Error");
        problemDetails.Detail.Should().Be("One or more validation errors occurred.");
        problemDetails.Errors.Should().NotBeNull();

        foreach (var propertyName in expectedPropertyNames)
        {
            problemDetails.Errors.Should().ContainKey(propertyName);
        }

        return problemDetails.Errors!;
    }

    /// <summary>
    /// Problem Details のレスポンス構造
    /// </summary>
    public class ProblemDetailsResponse
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public string? TraceId { get; set; }
    }
}