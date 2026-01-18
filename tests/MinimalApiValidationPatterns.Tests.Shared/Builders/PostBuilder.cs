using MinimalApiValidationPatterns.Entities;

namespace MinimalApiValidationPatterns.Tests.Shared.Builders;

/// <summary>
/// テストデータ作成用の Post ビルダー
/// </summary>
/// <remarks>
/// Test Data Builder パターンを使用して、テストで使用する Post オブジェクトを
/// 柔軟に構築します。デフォルト値を持ちつつ、必要に応じてカスタマイズできます。
/// </remarks>
/// <example>
/// <code>
/// // デフォルト値で作成
/// var post = new PostBuilder().Build();
/// 
/// // カスタマイズして作成
/// var post = new PostBuilder()
///     .WithTitle("Custom Title")
///     .WithContent("Custom Content")
///     .Build();
/// </code>
/// </example>
public class PostBuilder
{
    private string _title = "Default Test Title";
    private string _content = "Default Test Content";

    /// <summary>
    /// タイトルを設定
    /// </summary>
    public PostBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// コンテンツを設定
    /// </summary>
    public PostBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// 空のタイトルを設定（バリデーションエラーテスト用）
    /// </summary>
    public PostBuilder WithEmptyTitle()
    {
        _title = string.Empty;
        return this;
    }

    /// <summary>
    /// 空のコンテンツを設定（バリデーションエラーテスト用）
    /// </summary>
    public PostBuilder WithEmptyContent()
    {
        _content = string.Empty;
        return this;
    }

    /// <summary>
    /// 長いタイトルを設定（境界値テスト用）
    /// </summary>
    public PostBuilder WithLongTitle(int length = 1000)
    {
        _title = new string('A', length);
        return this;
    }

    /// <summary>
    /// 長いコンテンツを設定（境界値テスト用）
    /// </summary>
    public PostBuilder WithLongContent(int length = 10000)
    {
        _content = new string('B', length);
        return this;
    }

    /// <summary>
    /// Post オブジェクトを構築
    /// </summary>
    public Post Build()
    {
        return new Post
        {
            Title = _title,
            Content = _content
        };
    }
}