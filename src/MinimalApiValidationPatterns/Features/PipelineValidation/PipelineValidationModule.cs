using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiValidationPatterns.Data;
using MinimalApiValidationPatterns.Entities;

namespace MinimalApiValidationPatterns.Features.PipelineValidation;

/// <summary>
/// MediatR Pipeline Behavior パターンによるバリデーションモジュール
/// </summary>
/// <remarks>
/// MediatR を使用してCQRSパターンでバリデーションを実装します。
/// </remarks>
/// <para><strong>特徴:</strong></para>
/// <list type="bullet">
/// <item>CQRSパターンとの統合</item>
/// <item>全てのリクエストで自動的にバリデーション実行</item>
/// <item>複雑なビジネスロジックに適している</item>
/// <item>テストが容易</item>
/// </list> 
public sealed class PipelineValidationModule : ICarterModule
{
    /// <summary>
    /// エンドポイントルートを登録
    /// </summary>
    /// <param name="app">エンドポイントルートビルダー</param>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("/pipeline-behavior-posts")
            .WithTags("PipelineValidation");

        endpoints.MapGet("/", GetPosts)
            .WithSummary("Get all posts");

        endpoints.MapPost("/", CreatePost)
            .WithSummary("Create a new post")
            .ProducesValidationProblem();

        endpoints.MapPut("/", UpdatePost)
            .WithSummary("Update an existing post")
            .ProducesValidationProblem();
    }


    #region GetAll

    /// <summary>
    /// すべての投稿を取得するクエリ
    /// </summary>
    public record GetAllPostsQuery() : IRequest<GetPostsResponse>;

    /// <summary>
    /// 投稿のレスポンス
    /// </summary>
    public record PostResponse(Guid Id, string Title, string Content);

    /// <summary>
    /// すべての投稿のレスポンス
    /// </summary>
    public record GetPostsResponse(IEnumerable<PostResponse> Posts);

    /// <summary>
    /// すべての投稿を取得するハンドラ
    /// </summary>
    public class GetPostsHandler(InMemoryDatabase database) : IRequestHandler<GetAllPostsQuery, GetPostsResponse>
    {
        /// <summary>
        /// すべての投稿を取得する
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="ct">キャンセル用トークン</param>
        /// <returns>投稿のレスポンス</returns>
        public Task<GetPostsResponse> Handle(GetAllPostsQuery query, CancellationToken ct)
        {
            var response = new GetPostsResponse(
                database.Posts.Select(s => new PostResponse(s.Id, s.Title, s.Content)));
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// すべての投稿を取得する
    /// </summary>
    /// <param name="sender">MediatR の送信者</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>投稿のレスポンス</returns>
    public static async Task<Ok<GetPostsResponse>> GetPosts(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllPostsQuery(), ct);
        return TypedResults.Ok(result);
    }

    #endregion

    #region Create

    /// <summary>
    /// 投稿を作成するリクエスト
    /// </summary>
    public record CreatePostRequest(string Title, string Content) : IRequest<CreatePostResponse>;

    /// <summary>
    /// 投稿を作成するレスポンス
    /// </summary>
    public record CreatePostResponse(Guid Id);

    /// <summary>
    /// 投稿を作成するバリデータ
    /// </summary>
    public class CreatePostValidator : AbstractValidator<CreatePostRequest>
    {
        /// <summary>
        /// 投稿を作成するバリデータ
        /// </summary>
        public CreatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

    /// <summary>
    /// 投稿を作成するハンドラ
    /// </summary>
    public class CreatePostHandler(InMemoryDatabase database) : IRequestHandler<CreatePostRequest, CreatePostResponse>
    {
        /// <summary>
        /// 投稿を作成する
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        /// <returns>投稿のレスポンス</returns>
        public Task<CreatePostResponse> Handle(CreatePostRequest request, CancellationToken cancellationToken)
        {
            var post = new Post
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim()
            };

            database.Posts.Add(post);
            
            var response = new CreatePostResponse(post.Id);

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// 投稿を作成する
    /// </summary>
    /// <param name="sender">MediatR の送信者</param>
    /// <param name="request">リクエスト</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>投稿のレスポンス</returns>
    public static async Task<Ok<CreatePostResponse>> CreatePost(ISender sender, CreatePostRequest request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return TypedResults.Ok(result);
    }

    #endregion

    #region Update

    /// <summary>
    /// 投稿更新リクエスト
    /// </summary>
    public record UpdatePostRequest(string Title, string Content);

    /// <summary>
    /// 投稿更新コマンド
    /// </summary>
    public record UpdatePostCommand(Guid Id, string Title, string Content) : IRequest<UpdatePostResponse>;

    /// <summary>
    /// 投稿を作成するレスポンス
    /// </summary>
    public record UpdatePostResponse(Guid Id, string Title, string Content);

    /// <summary>
    /// 投稿更新バリデータ
    /// </summary>
    public class UpdatePostValidator : AbstractValidator<UpdatePostCommand>
    {
        /// <summary>
        /// 投稿更新バリデータ
        /// </summary>
        public UpdatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

    /// <summary>
    /// 投稿更新ハンドラ
    /// </summary>
    public class UpdatePostHandler(InMemoryDatabase database) : IRequestHandler<UpdatePostCommand, UpdatePostResponse>
    {
        /// <summary>
        /// 投稿を更新する
        /// </summary>
        /// <param name="request">リクエスト</param>
        /// <param name="cancellationToken">キャンセル用トークン</param>
        /// <returns>投稿のレスポンス</returns>
        public Task<UpdatePostResponse> Handle(UpdatePostCommand command, CancellationToken cancellationToken)
        {
            var post = database.Posts.FirstOrDefault(x => x.Id == command.Id)
                ?? throw new KeyNotFoundException("Post not found");
            post.Title = command.Title.Trim();
            post.Content = command.Content.Trim();

            var response = new UpdatePostResponse(post.Id, post.Title, post.Content);

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// 投稿を更新する
    /// </summary>
    /// <param name="sender">MediatR の送信者</param>
    /// <param name="request">リクエスト</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>投稿のレスポンス</returns>
    public static async Task<Results<Ok<UpdatePostResponse>, NotFound>> UpdatePost(ISender sender, Guid id, UpdatePostRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdatePostCommand(id, request.Title, request.Content);
            var result = await sender.Send(command, ct);

            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    #endregion
}
