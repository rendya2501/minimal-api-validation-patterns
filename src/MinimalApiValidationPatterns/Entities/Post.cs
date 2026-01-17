namespace MinimalApiValidationPatterns.Entities;

public class Post
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Content { get; set; }
}
