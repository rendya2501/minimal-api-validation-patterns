namespace MinimalApiValidationPatterns.Tests.Shared.Constants;

/// <summary>
/// テスト全体で使用する定数
/// </summary>
/// <remarks>
/// マジックナンバーやマジックストリングを避け、
/// テストの可読性と保守性を向上させます。
/// </remarks>
public static class TestConstants
{
    /// <summary>
    /// API エンドポイントパス
    /// </summary>
    public static class Endpoints
    {
        public const string FilterPosts = "/filter-posts/";
        public const string PipelinePosts = "/pipeline-behavior-posts/";
    }

    /// <summary>
    /// テストデータ
    /// </summary>
    public static class TestData
    {
        public const string ValidTitle = "Test Post Title";
        public const string ValidContent = "Test Post Content";
        public const int LongTextLength = 10000;
    }

    /// <summary>
    /// バリデーションメッセージ（期待されるエラーメッセージの一部）
    /// </summary>
    public static class ValidationMessages
    {
        public const string TitleRequired = "Title";
        public const string ContentRequired = "Content";
        public const string IdRequired = "Id";
    }

    /// <summary>
    /// HTTP ヘッダー
    /// </summary>
    public static class Headers
    {
        public const string ContentType = "Content-Type";
        public const string ProblemJsonMediaType = "application/problem+json";
        public const string JsonMediaType = "application/json";
    }
}