# Filter vs Pipeline Behavior - 詳細比較

## 概要

> **Filter**: お手軽で軽量、エンドポイント単位の制御  
> **Pipeline**: 最強の柔軟性、アプリケーション全体の統一

## 詳細比較表

| 観点 | Endpoint Filter | Pipeline Behavior | 勝者 |
|-----|----------------|-------------------|------|
| **学習コスト** | ⭐⭐⭐⭐⭐ 非常に低い | ⭐⭐⭐ MediatR の理解が必要 | Filter |
| **実装速度** | ⭐⭐⭐⭐⭐ 数分 | ⭐⭐⭐ セットアップに時間 | Filter |
| **パフォーマンス** | ⭐⭐⭐⭐⭐ オーバーヘッド最小 | ⭐⭐⭐⭐ MediatR のオーバーヘッド | Filter |
| **柔軟性** | ⭐⭐⭐ エンドポイント単位 | ⭐⭐⭐⭐⭐ 無限のカスタマイズ | Pipeline |
| **テスタビリティ** | ⭐⭐⭐ 統合テスト中心 | ⭐⭐⭐⭐⭐ ユニットテスト容易 | Pipeline |
| **保守性** | ⭐⭐⭐ エンドポイント増加で煩雑 | ⭐⭐⭐⭐⭐ 一元管理 | Pipeline |
| **拡張性** | ⭐⭐ 限定的 | ⭐⭐⭐⭐⭐ 無限 | Pipeline |

## 具体的な違い

### 1. コード量

#### Endpoint Filter
```csharp
// ✅ シンプル！エンドポイント定義時に1行追加するだけ
endpoints.MapPost("/posts", CreatePost)
    .WithRequestValidation<CreatePostRequest>();
```

#### Pipeline Behavior
```csharp
// Program.cs でグローバル設定（1回だけ）
builder.Services.AddMediatR(cfg => {
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// エンドポイント定義（MediatR を使うだけ）
endpoints.MapPost("/posts", async (ISender sender, CreatePostRequest req) => 
    await sender.Send(req));
```

### 2. バリデーション実行タイミングの制御

#### Endpoint Filter: エンドポイント単位で ON/OFF
```csharp
// ❌ この API だけバリデーションをスキップしたい場合...
endpoints.MapPost("/admin/posts", CreatePost)
    // .WithRequestValidation を付けない
    // → でも、忘れやすい！
```

#### Pipeline Behavior: 条件分岐で細かく制御
```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        // ✅ 特定のリクエストだけスキップ
        if (request is ISkipValidation)
            return await next(ct);

        // ✅ 管理者ユーザーはバリデーションを緩和
        if (_currentUser.IsAdmin)
            context = new ValidationContext<TRequest>(request) {
                RootContextData = { ["IsAdmin"] = true }
            };

        // ✅ 環境ごとに動作変更
        if (_env.IsProduction())
            // 本番環境のみ厳しいバリデーション
        
        // バリデーション実行...
    }
}
```

### 3. 複数のバリデーションステップ

#### Endpoint Filter: 複数フィルターを重ねる（限界あり）
```csharp
endpoints.MapPost("/posts", CreatePost)
    .WithRequestValidation<CreatePostRequest>()
    .WithAuthorizationValidation()      // カスタムフィルター
    .WithRateLimitValidation()          // カスタムフィルター
    .WithBusinessRuleValidation();      // カスタムフィルター
    // → フィルターの順序管理が難しい
```

#### Pipeline Behavior: パイプラインで自然に表現
```csharp
builder.Services.AddMediatR(cfg => {
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));         // 1. ロギング
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));      // 2. バリデーション
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));   // 3. 認可
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>));         // 4. キャッシュ
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));     // 5. トランザクション
});
// ✅ 実行順序が明確！
```

### 4. エラーハンドリングのカスタマイズ

#### Endpoint Filter: エンドポイントごとに異なる処理は困難
```csharp
public class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(...)
    {
        if (!result.IsValid)
        {
            // ❌ 常に同じ形式の ValidationProblem を返す
            return TypedResults.ValidationProblem(errors);
            
            // 💡 エンドポイントごとに変えたい場合は...？
            // → 複数のフィルタークラスを作る必要がある
        }
    }
}
```

#### Pipeline Behavior: リクエストタイプで柔軟に対応
```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        if (failures.Any())
        {
            // ✅ リクエストの種類で処理を変更
            if (request is IReturnCustomError customError)
            {
                throw customError.CreateException(failures);
            }
            
            // ✅ ドメインイベントを発行
            if (request is IPublishDomainEvents)
            {
                await _eventPublisher.Publish(
                    new ValidationFailedEvent(request, failures));
            }
            
            throw new ValidationException(failures);
        }
    }
}
```

## 実際のユースケース別推奨

### Endpoint Filter が最適

#### ✅ シンプルな CRUD API
```csharp
// Todo アプリなど、単純な操作のみ
endpoints.MapPost("/todos", CreateTodo)
    .WithRequestValidation<CreateTodoRequest>();
```

#### ✅ マイクロサービスの軽量エンドポイント
```csharp
// サービス間通信用の小さな API
endpoints.MapPost("/internal/notify", SendNotification)
    .WithRequestValidation<NotificationRequest>();
```

#### ✅ プロトタイプ・MVP 開発
```csharp
// とにかく速く作りたい！
endpoints.MapPost("/api/users", CreateUser)
    .WithRequestValidation<CreateUserRequest>();
```

### Pipeline Behavior が最適

#### ✅ エンタープライズアプリケーション
```csharp
// 複雑なビジネスルール、監査ログ、トランザクション管理
public record CreateOrderCommand : IRequest<OrderResult>
{
    // MediatR ハンドラーで処理
    // → 自動的に ValidationBehavior を通る
}
```

#### ✅ CQRS + Event Sourcing
```csharp
// Command と Query を完全分離
public record GetOrderQuery : IRequest<OrderDto> { }
public record UpdateOrderCommand : IRequest<Unit> { }

// ✅ Command のみバリデーション適用
public class ValidationBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        if (request is not ICommand)
            return await next(ct);  // Query はスキップ
        
        // Command のみバリデーション実行
    }
}
```

#### ✅ ドメイン駆動設計 (DDD)
```csharp
// Application Layer のパターン
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderId>
{
    public async Task<OrderId> Handle(...)
    {
        // ✅ バリデーションは ValidationBehavior で完了済み
        // ✅ トランザクションは TransactionBehavior で管理
        // → ハンドラーはビジネスロジックに専念！
        
        var order = Order.Create(...);
        await _repository.AddAsync(order);
        return order.Id;
    }
}
```

## 「最強」の構成: ハイブリッド

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => {
    // グローバル Pipeline Behavior
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
});

// エンドポイント定義
var api = app.MapGroup("/api");

// ✅ MediatR を使う複雑な操作 → Pipeline Behavior で自動バリデーション
api.MapPost("/orders", async (ISender sender, CreateOrderCommand cmd) =>
    await sender.Send(cmd));

// ✅ シンプルな操作 → Endpoint Filter で明示的バリデーション
api.MapGet("/health", () => Results.Ok("healthy"))
    .WithRequestValidation<HealthCheckRequest>();
```

## まとめ

### 🎯 選択基準

| プロジェクト規模 | チーム経験 | 推奨 |
|---------------|----------|------|
| 小規模（< 10 エンドポイント） | 初心者 | **Filter** |
| 中規模（10-50 エンドポイント） | 中級者 | **Pipeline** |
| 大規模（50+ エンドポイント） | 経験者 | **Pipeline** |
| マイクロサービス | - | **Filter**（単純）/**Pipeline**（複雑） |
| モノリス | - | **Pipeline** |

### 💡 最終結論

**「お手軽」vs「最強」ではなく「適材適所」**

- **Filter**: 速さ・シンプルさが必要な場面で無双
- **Pipeline**: 複雑さ・拡張性が必要な場面で真価を発揮

**両方使える技術力**を持つのが理想的！
