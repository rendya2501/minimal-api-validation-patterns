using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiValidationPatterns.Data;
using MinimalApiValidationPatterns.Entities;
using MinimalApiValidationPatterns.Filters;

namespace MinimalApiValidationPatterns.Features.FilterValidation;

public sealed class FilterValidationModule : ICarterModule
{
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

    public static Ok<IEnumerable<Post>> GetPosts(InMemoryDatabase database)
    {
        return TypedResults.Ok(database.Posts.AsEnumerable());
    }

    #endregion

    #region Create

    public record CreatePostRequest(string Title, string Content);

    public class CreatePostValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }

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

    public record UpdatePostRequest(Guid Id, string Title, string Content);
    public class UpdatePostValidator : AbstractValidator<UpdatePostRequest>
    {
        public UpdatePostValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
        }
    }
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
