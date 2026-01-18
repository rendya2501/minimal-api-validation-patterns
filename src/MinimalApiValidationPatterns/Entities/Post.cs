namespace MinimalApiValidationPatterns.Entities;

/// <summary>
/// 投稿エンティティ
/// </summary>
/// <remarks>
/// ブログ投稿やフォーラム投稿を表現するドメインエンティティ。
/// ID は作成時に自動生成され、変更不可です。
/// </remarks>
public class Post
{
    /// <summary>
    /// 投稿の一意識別子
    /// </summary>
    /// <remarks>
    /// private init により、オブジェクト初期化時にのみ設定可能。
    /// デフォルトで新しい GUID が自動生成されます。
    /// </remarks>
    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// 投稿のタイトル
    /// </summary>
    /// <remarks>
    /// 必須プロパティ（required）。空文字列は許可されません。
    /// </remarks>
    public required string Title { get; set; }

    /// <summary>
    /// 投稿の本文
    /// </summary>
    /// <remarks>
    /// 必須プロパティ（required）。空文字列は許可されません。
    /// </remarks>
    public required string Content { get; set; }
}
