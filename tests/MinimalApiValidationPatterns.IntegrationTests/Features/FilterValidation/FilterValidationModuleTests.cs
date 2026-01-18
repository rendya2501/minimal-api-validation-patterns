using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MinimalApiValidationPatterns.Entities;
using MinimalApiValidationPatterns.Fetures.FilterValidation;
using MinimalApiValidationPatterns.IntegrationTests.Infrastructure;
using MinimalApiValidationPatterns.Tests.Shared.Builders;
using MinimalApiValidationPatterns.Tests.Shared.Constants;
using MinimalApiValidationPatterns.Tests.Shared.Extensions;

namespace MinimalApiValidationPatterns.IntegrationTests.Features.FilterValidation;

/// <summary>
/// FilterValidationModule の統合テスト
/// </summary>
/// <remarks>
/// Endpoint Filter を使用したバリデーションの動作を検証します。
/// このテストでは、CustomWebApplicationFactory を使用して実際の HTTP リクエストを送信し、
/// エンドポイントの振る舞いを確認します。
/// </remarks>
public class FilterValidationModuleTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateTestClient();

    #region GetPosts Tests

    /// <summary>
    /// GET /filter-posts が正常に動作することを検証
    /// </summary>
    [Fact]
    public async Task GetPosts_ShouldReturnOk_WithPostList()
    {
        // Act
        var response = await _client.GetAsync(TestConstants.Endpoints.FilterPosts);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var posts = await response.Content.ReadFromJsonAsync<List<Post>>();
        posts.Should().NotBeNull();
        posts.Should().NotBeEmpty();
    }

    #endregion

    #region CreatePost Tests

    /// <summary>
    /// 有効なリクエストで POST /filter-posts が成功することを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new FilterValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.ValidTitle,
            Content: TestConstants.TestData.ValidContent
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var postId = await response.Content.ReadFromJsonAsync<Guid>();
        postId.Should().NotBeEmpty();
    }

    /// <summary>
    /// Title が空の場合にバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithEmptyTitle_ShouldReturnValidationError()
    {
        // Arrange
        var request = new FilterValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.EmptyString,
            Content: TestConstants.TestData.ValidContent
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        await response.ShouldHaveFilterValidationErrors(TestConstants.ValidationMessages.TitleRequired);
    }

    /// <summary>
    /// Content が空の場合にバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithEmptyContent_ShouldReturnValidationError()
    {
        // Arrange
        var request = new FilterValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.ValidTitle,
            Content: TestConstants.TestData.EmptyString
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        await response.ShouldHaveFilterValidationErrors(TestConstants.ValidationMessages.ContentRequired);
    }

    /// <summary>
    /// 複数のフィールドが無効な場合に全てのエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task CreatePost_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new FilterValidationModule.CreatePostRequest(
            Title: TestConstants.TestData.EmptyString,
            Content: TestConstants.TestData.EmptyString
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        await response.ShouldHaveFilterValidationErrors(
            TestConstants.ValidationMessages.TitleRequired,
            TestConstants.ValidationMessages.ContentRequired
        );
    }

    #endregion

    #region UpdatePost Tests

    /// <summary>
    /// 有効なリクエストで PUT /filter-posts が成功することを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithValidRequest_ShouldReturnOk()
    {
        // Arrange - まず投稿を作成
        var createRequest = new FilterValidationModule.CreatePostRequest(
            Title: "Original Title",
            Content: "Original Content"
        );
        var createResponse = await _client.PostAsJsonAsync(TestConstants.Endpoints.FilterPosts, createRequest);
        var postId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateRequest = new FilterValidationModule.UpdatePostRequest(
            Id: postId,
            Title: "Updated Title",
            Content: "Updated Content"
        );

        // Act
        var response = await _client.PutAsJsonAsync(TestConstants.Endpoints.FilterPosts, updateRequest);

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
        var request = new FilterValidationModule.UpdatePostRequest(
            Id: Guid.NewGuid(),
            Title: "Updated Title",
            Content: "Updated Content"
        );

        // Act
        var response = await _client.PutAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 空の ID でバリデーションエラーが返されることを検証
    /// </summary>
    [Fact]
    public async Task UpdatePost_WithEmptyId_ShouldReturnValidationError()
    {
        // Arrange
        var request = new FilterValidationModule.UpdatePostRequest(
            Id: Guid.Empty,
            Title: "Updated Title",
            Content: "Updated Content"
        );

        // Act
        var response = await _client.PutAsJsonAsync(TestConstants.Endpoints.FilterPosts, request);

        // Assert
        await response.ShouldHaveFilterValidationErrors(TestConstants.ValidationMessages.IdRequired);
    }

    #endregion
}