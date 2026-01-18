using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using MinimalApiValidationPatterns.Features.PipelineValidation;
using MinimalApiValidationPatterns.IntegrationTests.Infrastructure;
using MinimalApiValidationPatterns.Tests.Shared.Constants;
using MinimalApiValidationPatterns.Tests.Shared.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace MinimalApiValidationPatterns.IntegrationTests.Features.PipelineValidation;

/// <summary>
/// PipelineValidationModule の統合テスト
/// </summary>
/// <remarks>
/// MediatR Pipeline Behavior を使用したバリデーションの動作を検証します。
/// このアプローチでは、バリデーションがハンドラー実行前に自動的に行われ、
/// コントローラー層でのバリデーションロジックが不要になります。
/// </remarks>
public class PipelineValidationModuleTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateTestClient();

    #region GetPosts Tests

    /// <summary>
    /// GET /pipeline-behavior-posts が正常に動作することを検証
    /// </summary>
    [Fact]
    public async Task GetPosts_ShouldReturnOk_WithPostList()
    {
        // Act
        var response = await _client.GetAsync(TestConstants.Endpoints.PipelinePosts);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PipelineValidationModule.GetPostsResponse>();
        result.Should().NotBeNull();
        result!.Posts.Should().NotBeEmpty();
    }

    #endregion

    #region CreatePost Tests

    /// <summary>
    /// 有効なリクエストで POST /pipeline-behavior-posts が成功することを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new PipelineValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.ValidTitle,
            Content: TestConstants.TestData.ValidContent
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.PipelinePosts, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PipelineValidationModule.CreatePostResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    /// <summary>
    /// Title が空の場合にバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithEmptyTitle_ShouldReturnValidationError()
    {
        // Arrange
        var request = new PipelineValidationModule.CreatePostRequest(
            Title: string.Empty,
            Content: TestConstants.TestData.ValidContent
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.PipelinePosts, request);

        // Assert
        await response.ShouldHavePipelineValidationErrors(TestConstants.ValidationMessages.TitleRequired);
    }

    /// <summary>
    /// Content が空の場合にバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithEmptyContent_ShouldReturnValidationError()
    {
        // Arrange
        var request = new PipelineValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.ValidTitle,
            Content: string.Empty
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.PipelinePosts, request);

        // Assert
        await response.ShouldHavePipelineValidationErrors(TestConstants.ValidationMessages.ContentRequired);
    }

    /// <summary>
    /// Title と Content が空の場合にバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new PipelineValidationModule.CreatePostRequest(
            Title: string.Empty,
            Content: string.Empty
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.PipelinePosts, request);

        // Assert
        await response.ShouldHavePipelineValidationErrors(
            TestConstants.ValidationMessages.TitleRequired,
            TestConstants.ValidationMessages.ContentRequired
        );
    }

    /// <summary>
    /// ValidationBehavior が Global Exception Handler と連携して
    /// 適切な Problem Details を返すことを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_ValidationError_ShouldReturnProblemDetails()
    {
        // Arrange
        var request = new PipelineValidationModule.CreatePostRequest(
            Title: string.Empty,
            Content: string.Empty
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.PipelinePosts, request);

        // Assert
        var problemDetails = await response.ShouldBeProblemDetails();
        problemDetails.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problemDetails.TraceId.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpdatePost Tests

    /// <summary>
    /// 有効なリクエストで PUT /pipeline-behavior-posts が成功することを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithValidRequest_ShouldReturnOk()
    {
        // Arrange - まず投稿を作成
        var createRequest = new PipelineValidationModule.CreatePostRequest(
            Title: "Original Title",
            Content: "Original Content"
        );
        var createResponse = await _client.PostAsJsonAsync("/pipeline-behavior-posts/", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<PipelineValidationModule.CreatePostResponse>();

        var updateRequest = new PipelineValidationModule.UpdatePostRequest(
            Title: "Updated Title",
            Content: "Updated Content"
        );
        var query = new Dictionary<string, string?>
        {
            { "id", createResult!.Id.ToString() }
        };
        // ベースURLにクエリパラメータを結合
        var uri = QueryHelpers.AddQueryString("/pipeline-behavior-posts/", query);

        // Act
        var response = await _client.PutAsJsonAsync(uri, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// 存在しない ID で更新した場合に NotFound が返されることを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new PipelineValidationModule.UpdatePostRequest(
            Title: "Updated Title",
            Content: "Updated Content"
        );
        var query = new Dictionary<string, string?>
        {
            { "id", Guid.NewGuid().ToString() }
        };
        // ベースURLにクエリパラメータを結合
        var uri = QueryHelpers.AddQueryString("/pipeline-behavior-posts/", query);

        // Act
        var response = await _client.PutAsJsonAsync(uri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 空の ID で Pipeline Behavior から NotFound が返されることを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithEmptyId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new PipelineValidationModule.UpdatePostRequest(
            Title: "Updated Title",
            Content: "Updated Content"
        );
        var query = new Dictionary<string, string?>
        {
            { "id", Guid.Empty.ToString() }
        };
        // ベースURLにクエリパラメータを結合
        var uri = QueryHelpers.AddQueryString("/pipeline-behavior-posts/", query);

        // Act
        var response = await _client.PutAsJsonAsync(uri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 更新時に複数のバリデーションエラーが正しく処理されることを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        // Arrange - まず投稿を作成
        var createRequest = new PipelineValidationModule.CreatePostRequest(
           Title: "Original Title",
           Content: "Original Content"
       );
        var createResponse = await _client.PostAsJsonAsync("/pipeline-behavior-posts/", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<PipelineValidationModule.CreatePostResponse>();

        var query = new Dictionary<string, string?>
        {
            { "id", createResult!.Id.ToString() }
        };

        var updateUri = QueryHelpers.AddQueryString("/pipeline-behavior-posts/", query);

        var updateRequest = new PipelineValidationModule.UpdatePostRequest(
            Title: string.Empty,
            Content: string.Empty
        );

        // Act
        var response = await _client.PutAsJsonAsync(updateUri, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Title");
        problemDetails.Should().Contain("Content");
    }

    #endregion
}