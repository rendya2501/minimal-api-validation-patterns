using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiValidationPatterns.Data;
using MinimalApiValidationPatterns.Entities;

namespace MinimalApiValidationPatterns.Features.PipelineValidation;

public sealed class PipelineValidationModule : ICarterModule
{
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

    public record GetAllGamesQuery() : IRequest<GetPostsResponse>;
    public record PostResponse(Guid Id, string Title, string Content);
    public record GetPostsResponse(IEnumerable<PostResponse> Posts);

    public class GetPostsHandler(InMemoryDatabase database) : IRequestHandler<GetAllGamesQuery, GetPostsResponse>
    {
        public async Task<GetPostsResponse> Handle(GetAllGamesQuery query, CancellationToken ct)
        {
            return new GetPostsResponse(database.Posts.Select(s => new PostResponse(s.Id, s.Title, s.Content)));
        }
    }
    public static async Task<Ok<GetPostsResponse>> GetPosts(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllGamesQuery(), ct);
        return TypedResults.Ok(result);
    }

    //public static List<Post> GetPosts(InMemoryDatabase database)
    //{
    //    return database.Posts;
    //}

    #endregion

    #region Create

    public record CreatePostRequest(string Title, string Content) : IRequest<CreatePostResponse>;
    public record CreatePostResponse(Guid Id);
    public class CreatePostValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }
    public class CreatePostHandler(InMemoryDatabase database) : IRequestHandler<CreatePostRequest, CreatePostResponse>
    {
        public async Task<CreatePostResponse> Handle(CreatePostRequest request, CancellationToken cancellationToken)
        {
            var post = new Post
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim()
            };

            database.Posts.Add(post);
            return new CreatePostResponse(post.Id);
        }
    }
    public static async Task<Ok<CreatePostResponse>> CreatePost(ISender sender, CreatePostRequest request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return TypedResults.Ok(result);
    }

    //public static Ok<Guid> CreatePost(CreatePostRequest request, InMemoryDatabase database)
    //{
    //    var post = new Post
    //    {
    //        Title = request.Title.Trim(),
    //        Content = request.Content.Trim()
    //    };

    //    database.Posts.Add(post);
    //    return TypedResults.Ok(post.Id);
    //}

    #endregion

    #region Update
    public record UpdatePostRequest(Guid Id, string Title, string Content) : IRequest<UpdatePostResponse>;
    public record UpdatePostResponse;
    public class UpdatePostValidator : AbstractValidator<UpdatePostRequest>
    {
        public UpdatePostValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }
    public class UpdatePostHandler(InMemoryDatabase database) : IRequestHandler<UpdatePostRequest, UpdatePostResponse>
    {
        public async Task<UpdatePostResponse> Handle(UpdatePostRequest request, CancellationToken cancellationToken)
        {
            var post = database.Posts.FirstOrDefault(x => x.Id == request.Id)
                ?? throw new KeyNotFoundException("Post not found");
            post.Title = request.Title.Trim();
            post.Content = request.Content.Trim();
            return new UpdatePostResponse();
        }
    }
    public static async Task<Results<Ok<UpdatePostResponse>, NotFound>> UpdatePost(ISender sender, UpdatePostRequest request, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(request, ct);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    //public static Results<Ok, NotFound> UpdatePost(UpdatePostRequest request, InMemoryDatabase database)
    //{
    //    var post = database.Posts.FirstOrDefault(x => x.Id == request.Id);

    //    if (post is null)
    //    {
    //        return TypedResults.NotFound();
    //    }

    //    post.Title = request.Title.Trim();
    //    post.Content = request.Content.Trim();
    //    return TypedResults.Ok();
    //}

    #endregion
}
