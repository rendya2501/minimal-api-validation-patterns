using MinimalApiValidationPatterns.Entities;

namespace MinimalApiValidationPatterns.Data;

public class InMemoryDatabase
{
    public List<Post> Posts => field ??= [new Post() { Title = "hoge", Content = "fuga" }];
}
