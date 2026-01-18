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
/// MediatR の CQRS パターンを使用し、
/// <see cref="ValidationBehavior{TRequest, TResponse}"/> により
/// 自動的にバリデーションを実行します。
/// <para>
/// <strong>特徴:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>アプリケーション層での一元的なバリデーション</description></item>
/// <item><description>すべてのリクエストで自動実行</description></item>
/// <item><description>CQRS パターンとの統合</description></item>
/// <item><description>複雑なビジネスロジックに最適</description></item>
/// <item><description>テストが容易</description></item>
/// </list>
/// </remarks>
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
            .WithSummary("Get all posts")
            .WithDescription("すべての投稿を取得します");

        endpoints.MapPost("/", CreatePost)
            .WithSummary("Create a new post")
            .WithDescription("新しい投稿を作成します")
            .ProducesValidationProblem();

        endpoints.MapPut("/", UpdatePost)
            .WithSummary("Update an existing post")
            .WithDescription("既存の投稿を更新します")
            .ProducesValidationProblem();
    }

    #region GetAll

    /// <summary>
    /// すべての投稿を取得するクエリ
    /// </summary>
    public record GetAllPostsQuery() : IRequest<GetPostsResponse>;

    /// <summary>
    /// 投稿レスポンス
    /// </summary>
    /// <param name="Id">投稿ID</param>
    /// <param name="Title">タイトル</param>
    /// <param name="Content">本文</param>
    public record PostResponse(Guid Id, string Title, string Content);

    /// <summary>
    /// 投稿一覧レスポンス
    /// </summary>
    /// <param name="Posts">投稿のコレクション</param>
    public record GetPostsResponse(IEnumerable<PostResponse> Posts);

    /// <summary>
    /// <see cref="GetAllPostsQuery"/> のハンドラ
    /// </summary>
    public class GetPostsHandler(InMemoryDatabase database)
        : IRequestHandler<GetAllPostsQuery, GetPostsResponse>
    {
        /// <summary>
        /// クエリを処理
        /// </summary>
        public Task<GetPostsResponse> Handle(
            GetAllPostsQuery query,
            CancellationToken ct)
        {
            var response = new GetPostsResponse(
                database.Posts.Select(s => new PostResponse(s.Id, s.Title, s.Content)));
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// すべての投稿を取得するエンドポイント
    /// </summary>
    public static async Task<Ok<GetPostsResponse>> GetPosts(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAllPostsQuery(), ct);
        return TypedResults.Ok(result);
    }

    #endregion

    #region Create

    /// <summary>
    /// 投稿作成コマンド
    /// </summary>
    /// <param name="Title">投稿のタイトル（必須）</param>
    /// <param name="Content">投稿の本文（必須）</param>
    public record CreatePostRequest(string Title, string Content)
        : IRequest<CreatePostResponse>;

    /// <summary>
    /// 投稿作成レスポンス
    /// </summary>
    /// <param name="Id">作成された投稿のID</param>
    public record CreatePostResponse(Guid Id);

    /// <summary>
    /// <see cref="CreatePostRequest"/> のバリデータ
    /// </summary>
    public class CreatePostValidator : AbstractValidator<CreatePostRequest>
    {
        /// <summary>
        /// コンストラクタ - バリデーションルールを定義
        /// </summary>
        public CreatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

    /// <summary>
    /// <see cref="CreatePostRequest"/> のハンドラ
    /// </summary>
    public class CreatePostHandler(InMemoryDatabase database)
        : IRequestHandler<CreatePostRequest, CreatePostResponse>
    {
        /// <summary>
        /// コマンドを処理
        /// </summary>
        /// <remarks>
        /// バリデーションは <see cref="ValidationBehavior{TRequest, TResponse}"/> 
        /// により事前に実行されます。
        /// </remarks>
        public Task<CreatePostResponse> Handle(
            CreatePostRequest request,
            CancellationToken cancellationToken)
        {
            var post = new Post
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim()
            };

            database.Posts.Add(post);
            return Task.FromResult(new CreatePostResponse(post.Id));
        }
    }

    /// <summary>
    /// 新しい投稿を作成するエンドポイント
    /// </summary>
    /// <param name="sender">MediatR の送信者</param>
    /// <param name="request">リクエスト</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>投稿のレスポンス</returns>
    public static async Task<Ok<CreatePostResponse>> CreatePost(
        ISender sender,
        CreatePostRequest request,
        CancellationToken ct)
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
    /// <param name="Id">更新対象の投稿ID（必須）</param>
    /// <param name="Title">新しいタイトル（必須）</param>
    /// <param name="Content">新しい本文（必須）</param>
    public record UpdatePostCommand(Guid Id, string Title, string Content) : IRequest<UpdatePostResponse>;

    /// <summary>
    /// 投稿更新レスポンス
    /// </summary>
    /// <param name="Id">更新後の投稿ID</param>
    /// <param name="Title">更新後のタイトル</param>
    /// <param name="Content">更新後の本文</param>
    public record UpdatePostResponse(Guid Id, string Title, string Content);

    /// <summary>
    /// <see cref="UpdatePostCommand"/> のバリデータ
    /// </summary>
    public class UpdatePostValidator : AbstractValidator<UpdatePostCommand>
    {
        /// <summary>
        /// コンストラクタ - バリデーションルールを定義
        /// </summary>
        public UpdatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

    /// <summary>
    /// <see cref="UpdatePostCommand"/> のハンドラ
    /// </summary>
    public class UpdatePostHandler(InMemoryDatabase database) : IRequestHandler<UpdatePostCommand, UpdatePostResponse>
    {
        /// <summary>
        /// コマンドを処理
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// 指定されたIDの投稿が見つからない場合
        /// </exception>
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
    /// 既存の投稿を更新するエンドポイント
    /// </summary>
    /// <param name="sender">MediatR の送信者</param>
    /// <param name="request">リクエスト</param>
    /// <param name="ct">キャンセル用トークン</param>
    /// <returns>投稿のレスポンス</returns>
    public static async Task<Results<Ok<UpdatePostResponse>, NotFound>> UpdatePost(
        ISender sender,
        Guid id,
        UpdatePostRequest request,
        CancellationToken ct)
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
