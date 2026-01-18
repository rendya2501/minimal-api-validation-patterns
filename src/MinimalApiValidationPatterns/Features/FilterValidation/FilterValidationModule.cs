using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiValidationPatterns.Data;
using MinimalApiValidationPatterns.Entities;
using MinimalApiValidationPatterns.Filters;

namespace MinimalApiValidationPatterns.Features.FilterValidation;

/// <summary>
/// Endpoint Filter パターンによるバリデーションモジュール
/// </summary>
/// <remarks>
/// Carter を使用してエンドポイントを定義し、
/// ValidationFilter を通じてリクエストをバリデーションします。
/// 
/// <para><strong>特徴:</strong></para>
/// <list type="bullet">
/// <item>エンドポイントレベルでのバリデーション制御</item>
/// <item>明示的な .WithRequestValidation() による適用</item>
/// <item>軽量でシンプルな実装</item>
/// </list>
/// </remarks>
public sealed class FilterValidationModule : ICarterModule
{
    /// <summary>
    /// エンドポイントルートを登録
    /// </summary>
    /// <param name="app">エンドポイントルートビルダー</param>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("/filter-posts")
            .WithTags("FilterValidation"); ;

        endpoints.MapGet("/", GetPosts)
            .WithSummary("Get all posts");

        endpoints.MapPost("/", CreatePost)
            .WithSummary("Create a new post")
            .WithRequestValidation<CreatePostRequest>();

        endpoints.MapPut("/", UpdatePost)
            .WithSummary("Update an existing post")
            .WithRequestValidation<UpdatePostRequest>();
    }


    #region GetAll

    /// <summary>
    /// 全投稿を取得
    /// </summary>
    /// <param name="database">インメモリデータベース</param>
    /// <returns>投稿のコレクション</returns>
    public static Ok<IEnumerable<Post>> GetPosts(InMemoryDatabase database)
    {
        return TypedResults.Ok(database.Posts.AsEnumerable());
    }

    #endregion

    #region Create

    /// <summary>
    /// 投稿作成リクエスト
    /// </summary>
    /// <param name="Title">投稿のタイトル</param>
    /// <param name="Content">投稿の本文</param>
    public record CreatePostRequest(string Title, string Content);

    /// <summary>
    /// CreatePostRequest のバリデータ
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
    /// 新しい投稿を作成
    /// </summary>
    /// <param name="request">作成リクエスト</param>
    /// <param name="database">インメモリデータベース</param>
    /// <returns>作成された投稿のID</returns>
    /// <remarks>
    /// バリデーションは ValidationFilter により事前に実行されます。
    /// </remarks>
    public static Ok<Guid> CreatePost(CreatePostRequest request, InMemoryDatabase database)
    {
        var post = new Post
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim()
        };

        database.Posts.Add(post);
        return TypedResults.Ok(post.Id);
    }

    #endregion

    #region Update

    /// <summary>
    /// 投稿更新リクエスト
    /// </summary>
    /// <param name="Id">更新対象の投稿ID</param>
    /// <param name="Title">新しいタイトル</param>
    /// <param name="Content">新しい本文</param>
    public record UpdatePostRequest(Guid Id, string Title, string Content);

    /// <summary>
    /// UpdatePostRequest のバリデータ
    /// </summary>
    public class UpdatePostValidator : AbstractValidator<UpdatePostRequest>
    {
        /// <summary>
        /// コンストラクタ - バリデーションルールを定義
        /// </summary>
        public UpdatePostValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

    /// <summary>
    /// 既存の投稿を更新
    /// </summary>
    /// <param name="request">更新リクエスト</param>
    /// <param name="database">インメモリデータベース</param>
    /// <returns>成功時は Ok、投稿が見つからない場合は NotFound</returns>
    public static Results<Ok, NotFound> UpdatePost(UpdatePostRequest request, InMemoryDatabase database)
    {
        var post = database.Posts.FirstOrDefault(x => x.Id == request.Id);

        if (post is null)
        {
            return TypedResults.NotFound();
        }

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        return TypedResults.Ok();
    }

    #endregion
}
