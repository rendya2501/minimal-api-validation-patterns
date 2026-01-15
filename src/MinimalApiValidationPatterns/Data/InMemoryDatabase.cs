using RequestValidationInMinimalAPIs.Entities;

namespace RequestValidationInMinimalAPIs.Data;

public class InMemoryDatabase
{
    public List<Post> Posts => field ??= [new Post() { Title = "hoge", Content = "fuga" }];
}
