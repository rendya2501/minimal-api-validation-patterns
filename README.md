# MinimalApiValidationPatterns

ASP.NET Core Minimal API における2つのバリデーションパターンの実装例です。

## 概要

このプロジェクトは、Minimal API でバリデーションを実装する2つの主要なアプローチを示しています:

1. **Endpoint Filter によるバリデーション** - エンドポイントレベルでのバリデーション
2. **MediatR Pipeline Behavior によるバリデーション** - アプリケーション層でのバリデーション

両方のアプローチとも FluentValidation を使用し、RFC 9110 準拠の Problem Details 形式でエラーを返します。

## アーキテクチャ

### 1. Endpoint Filter パターン (`/filter-posts`)

```
HTTP Request → Endpoint Filter → Validation → Handler → Response
```

**特徴:**
- エンドポイントごとに明示的にバリデーションを適用
- 軽量で理解しやすい
- エンドポイント固有のバリデーションに適している

**実装:**
```csharp
endpoints.MapPost("/", CreatePost)
    .WithRequestValidation<CreatePostRequest>();
```

### 2. Pipeline Behavior パターン (`/pipeline-behavior-posts`)

```
HTTP Request → Endpoint → MediatR → ValidationBehavior → Handler → Response
```

**特徴:**
- CQRS パターンとの統合
- 全てのリクエストで自動的にバリデーション実行
- 複雑なビジネスロジックに適している
- テストが容易

**実装:**
```csharp
// Program.cs でグローバルに設定
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```

## プロジェクト構造

```
src/MinimalApiValidationPatterns/
├── Behaviors/
│   └── ValidationBehavior.cs          # MediatR パイプライン動作
├── Data/
│   └── InMemoryDatabase.cs            # シンプルなインメモリDB
├── Entities/
│   └── Post.cs                        # ドメインエンティティ
├── ExceptionHandling/
│   ├── GlobalExceptionHandler.cs     # グローバル例外ハンドラー
│   ├── NotFoundException.cs          # カスタム例外
│   └── ErrorContext.cs               # エラーコンテキスト
├── Features/
│   ├── FilterValidation/             # Filter パターンの実装
│   │   └── FilterValidationModule.cs
│   └── PipelineValidation/           # Pipeline パターンの実装
│       └── PipelineValidationModule.cs
└── Filters/
    └── ValidationFilter.cs            # エンドポイントフィルター

tests/
├── MinimalApiValidationPatterns.UnitTests/         # ユニットテスト
│   ├── Behaviors/
│   │   └── ValidationBehaviorTests.cs
│   ├── Filters/
│   │   └── ValidationFilterTests.cs
│   └── ExceptionHandling/
│       └── GlobalExceptionHandlerTests.cs
│
├── MinimalApiValidationPatterns.IntegrationTests/  # 統合テスト 
│   ├── Features/
│   │   ├── FilterValidation/
│   │   │   └── FilterValidationModuleTests.cs
│   │   └── PipelineValidation/
│   │       └── PipelineValidationModuleTests.cs
│   └── Infrastructure/
│       └── CustomWebApplicationFactory.cs
│
└── MinimalApiValidationPatterns.Tests.Shared/      # 共通項目
    ├── Builders/
    │   └── PostBuilder.cs             # テストデータビルダー
    ├── Constants/
    │   └── TestConstants.cs           # 共通定数
    └── Extensions/
        └── HttpResponseExtensions.cs   # 共通拡張メソッド

docs/
├── FilterVsPipeline.md                # パターン選択ガイド
└── Troubleshooting.md                 # よくある問題と解決策
```

## 技術スタック

- **.NET 10.0** - 最新の .NET フレームワーク
- **FluentValidation 12.1.1** - 宣言的バリデーションライブラリ
- **MediatR 14.0.0** - CQRS パターン実装
- **Carter 10.0.0** - Minimal API エンドポイント定義
- **Scalar** - API ドキュメント生成
- **xUnit** - テストフレームワーク
- **FluentAssertions** - テスト検証ライブラリ

## セットアップ

### 必要要件

- .NET 10.0 SDK

### インストール

```bash
# リポジトリをクローン
git clone <repository-url>
cd MinimalApiValidationPatterns

# 依存関係の復元
dotnet restore

# アプリケーション実行
dotnet run --project src/MinimalApiValidationPatterns

# テスト実行
dotnet test
```

### API ドキュメント

アプリケーション起動後、以下の URL で API ドキュメントにアクセスできます:

- Scalar UI: `http://localhost:5207/scalar/v1`
- OpenAPI: `http://localhost:5207/openapi/v1.json`

## API エンドポイント

### Filter Validation パターン

| メソッド | エンドポイント | 説明 |
|---------|--------------|------|
| GET | `/filter-posts/` | 全投稿取得 |
| POST | `/filter-posts/` | 新規投稿作成 |
| PUT | `/filter-posts/` | 投稿更新 |

### Pipeline Behavior パターン

| メソッド | エンドポイント | 説明 |
|---------|--------------|------|
| GET | `/pipeline-behavior-posts/` | 全投稿取得 |
| POST | `/pipeline-behavior-posts/` | 新規投稿作成 |
| PUT | `/pipeline-behavior-posts/` | 投稿更新 |

## バリデーションルール

### CreatePostRequest

```csharp
public record CreatePostRequest(string Title, string Content);

// バリデーションルール:
// - Title: 必須
// - Content: 必須
```

### UpdatePostRequest

```csharp
public record UpdatePostRequest(Guid Id, string Title, string Content);

// バリデーションルール:
// - Id: 必須、空のGUID不可
// - Title: 必須
// - Content: 必須
```

## エラーレスポンス

全てのバリデーションエラーは RFC 9110 準拠の Problem Details 形式で返されます:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/filter-posts/",
  "errors": {
    "Title": ["'Title' must not be empty."],
    "Content": ["'Content' must not be empty."]
  },
  "traceId": "00-abc123..."
}
```

## 例外処理

グローバル例外ハンドラーが以下の例外を処理します:

| 例外タイプ | ステータスコード | 説明 |
|-----------|----------------|------|
| `ValidationException` | 400 | FluentValidation のバリデーションエラー |
| `NotFoundException` | 404 | リソースが見つからない |
| `UnauthorizedAccessException` | 401 | 認証が必要 |
| `ArgumentException` | 400 | 引数エラー |
| `InvalidOperationException` | 400 | 無効な操作 |
| `OperationCanceledException` | 499 | リクエストキャンセル |
| その他の例外 | 500 | 内部サーバーエラー |

## テスト

### テストの実行

```bash
# 全テスト実行
dotnet test

# ユニットテストのみ（高速）
dotnet test tests/MinimalApiValidationPatterns.UnitTests

# 統合テストのみ
dotnet test tests/MinimalApiValidationPatterns.IntegrationTests

# カバレッジ付きテスト
dotnet test /p:CollectCoverage=true

# 特定のテストクラスのみ実行
dotnet test --filter FullyQualifiedName~FilterValidationModuleTests
```

### テストの種類

1. **ユニットテスト**
   - `ValidationBehaviorTests` - Pipeline Behavior のロジック検証
   - `ValidationFilterTests` - Endpoint Filter のロジック検証

2. **統合テスト**
   - `FilterValidationModuleTests` - Filter パターンのエンドツーエンドテスト
   - `PipelineValidationModuleTests` - Pipeline パターンのエンドツーエンドテスト
   - `GlobalExceptionHandlerTests` - 例外処理の検証

## どちらのパターンを選ぶべきか

このプロジェクトは2つのバリデーションパターンを実装していますが、実際のプロジェクトではどちらか一方を選択することを推奨します。

詳細な比較と選択ガイドは **[docs/FilterVsPipeline.md](docs/FilterVsPipeline.md)** を参照してください。

### クイックガイド

| プロジェクト規模 | チーム経験 | 推奨 |
|---------------|----------|------|
| 小規模（< 10 エンドポイント） | 初心者 | **Filter** |
| 中規模（10-50 エンドポイント） | 中級者 | **Pipeline** |
| 大規模（50+ エンドポイント） | 経験者 | **Pipeline** |
| マイクロサービス（単純） | - | **Filter** |
| エンタープライズ・CQRS | - | **Pipeline** |

## パフォーマンス考慮事項

- **Endpoint Filter**: より軽量、エンドポイント単位で適用
- **Pipeline Behavior**: MediatR のオーバーヘッドがあるが、大規模アプリケーションでは構造的な利点が大きい

## 開発環境と本番環境の違い

グローバル例外ハンドラーは環境に応じて異なる情報を返します:

**開発環境:**
- スタックトレース
- 例外の詳細
- 内部例外情報

**本番環境:**
- ユーザーフレンドリーなエラーメッセージのみ
- 機密情報は含まれない

## 参考資料

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [ASP.NET Core Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [RFC 9110 - HTTP Semantics](https://tools.ietf.org/html/rfc9110)
- [Problem Details for HTTP APIs (RFC 7807)](https://tools.ietf.org/html/rfc7807)
- [Request Validation in .NET / C# Minimal APIs](https://www.youtube.com/watch?v=1qJTVcR1VN8)  
- Original sample by [jonowilliams26](https://github.com/jonowilliams26/youtube-videos/tree/main/RequestValidationInMinimalAPIs)
