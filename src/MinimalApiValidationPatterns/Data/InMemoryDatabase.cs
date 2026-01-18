using MinimalApiValidationPatterns.Entities;

namespace MinimalApiValidationPatterns.Data;

/// <summary>
/// インメモリデータベース
/// </summary>
/// <remarks>
/// テスト・デモ用の簡易データベース実装。
/// 本番環境では EF Core などの永続化層に置き換えてください。
/// スレッドセーフではないため、単一インスタンスでの利用を想定しています。
/// </remarks>
public class InMemoryDatabase
{
    /// <summary>
    /// 投稿のコレクション
    /// </summary>
    /// <remarks>
    /// C# 13 の field キーワードを使用した自動プロパティ初期化。
    /// 初回アクセス時にデフォルトデータで初期化されます。
    /// </remarks>
    public List<Post> Posts => field ??= [new Post() { Title = "hoge", Content = "fuga" }];
}
