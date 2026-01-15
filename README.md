# Minimal API Validation Patterns

ASP.NET Core Minimal API における FluentValidation の実装パターン比較プロジェクト

## 比較する2つのアプローチ

### 1. Filter Pattern (`/filter-validation`)
- `IEndpointFilter` を使用したバリデーション
- エンドポイントレベルでのバリデーション
- 軽量でシンプル

### 2. Pipeline Behavior Pattern (`/pipeline-validation`)
- MediatR の `IPipelineBehavior` を使用
- アプリケーション層でのバリデーション
- CQRS パターンと相性が良い

## 技術スタック
- ASP.NET Core 10 Minimal API
- FluentValidation
- MediatR
- Carter

## エンドポイント

| パターン | エンドポイント |
|---------|--------------|
| Filter | `/filter-validation/*` |
| Pipeline | `/pipeline-validation/*` |

## 各パターンの特徴

### Filter Pattern
- シンプル
- Minimal API に最適化
- MediatR 不要な場合の選択肢

### Pipeline Behavior Pattern
- CQRS との統合
- 横断的関心事の統一
- やや複雑

## 参考

[Request Validation in .NET / C# Minimal APIs](https://www.youtube.com/watch?v=1qJTVcR1VN8)  
Original sample by [jonowilliams26](https://github.com/jonowilliams26/youtube-videos/tree/main/RequestValidationInMinimalAPIs)

