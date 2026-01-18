using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalApiValidationPatterns.Data;

namespace MinimalApiValidationPatterns.IntegrationTests.Infrastructure;

/// <summary>
/// 統合テスト用のカスタム WebApplicationFactory
/// </summary>
/// <remarks>
/// テスト環境に特化した設定を行い、実際のアプリケーションと
/// 分離されたテスト環境を構築します。
/// 各テストクラスで共有されることを想定しています。
/// </remarks>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Web ホストの設定をカスタマイズ
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // テスト用にサービスを置き換える場合はここで実装
            // 例: InMemoryDatabase をテスト専用のものに置き換え

            // 既存の InMemoryDatabase を削除
            services.RemoveAll<InMemoryDatabase>();

            // テスト用の新しい InMemoryDatabase を登録
            services.AddSingleton<InMemoryDatabase>();
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// テスト用のクライアントを作成
    /// </summary>
    /// <remarks>
    /// 必要に応じてデフォルトのリクエストヘッダーなどを設定できます
    /// </remarks>
    public HttpClient CreateTestClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // 共通のヘッダーを設定（必要な場合）
        // client.DefaultRequestHeaders.Add("X-Test-Header", "TestValue");

        return client;
    }
}