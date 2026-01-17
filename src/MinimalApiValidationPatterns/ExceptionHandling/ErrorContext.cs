namespace MinimalApiValidationPatterns.ExceptionHandling;

/// <summary>
/// エラーコンテキスト（内部利用）
/// </summary>
internal record ErrorContext(
    int StatusCode,
    string Title,
    string Detail,
    IDictionary<string, string[]>? ValidationErrors = null,
    bool SuppressBody = false
);
