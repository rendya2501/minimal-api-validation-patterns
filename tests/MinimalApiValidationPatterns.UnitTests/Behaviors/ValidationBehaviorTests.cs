using FluentAssertions;
using FluentValidation;
using MediatR;
using MinimalApiValidationPatterns.Behaviors;

namespace MinimalApiValidationPatterns.Tests.Behaviors;

/// <summary>
/// ValidationBehavior のユニットテスト
/// </summary>
/// <remarks>
/// MediatR Pipeline Behavior としての ValidationBehavior の動作を検証します。
/// このクラスは、バリデーションロジックが正しく実行され、適切な例外が
/// スローされることを確認します。
/// </remarks>
public class ValidationBehaviorTests
{
    /// <summary>
    /// テスト用のリクエストレコード
    /// </summary>
    private record TestRequest(string Name, int Age) : IRequest<TestResponse>;

    /// <summary>
    /// テスト用のレスポンスレコード
    /// </summary>
    private record TestResponse(bool Success);

    /// <summary>
    /// テスト用のバリデータ
    /// </summary>
    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0");
        }
    }

    /// <summary>
    /// バリデータが存在しない場合、次の処理が実行されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("John", 25);
        var nextCalled = false;

        Task<TestResponse> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse(true));
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// 有効なリクエストの場合、バリデーションを通過して次の処理が実行されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("John", 25);
        var nextCalled = false;

        Task<TestResponse> Next(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse(true));
        }

        // Act
        var result = await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// 無効なリクエストの場合、ValidationException がスローされることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", -1); // 両方のフィールドが無効

        Task<TestResponse> Next(CancellationToken ct)
        {
            return Task.FromResult(new TestResponse(true));
        }

        // Act
        Func<Task> act = async () => await behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// 無効なリクエストの場合、全てのバリデーションエラーが含まれることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldContainAllValidationErrors()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", -1); // 両方のフィールドが無効

        Task<TestResponse> Next(CancellationToken ct)
        {
            return Task.FromResult(new TestResponse(true));
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await behavior.Handle(request, Next, CancellationToken.None)
        );

        exception.Errors.Should().HaveCount(2);
        exception.Errors.Should().Contain(e => e.PropertyName == "Name");
        exception.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    /// <summary>
    /// 複数のバリデータが存在する場合、全てが実行されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldExecuteAll()
    {
        // Arrange
        var validator1 = new TestRequestValidator();
        var validator2 = new AdditionalTestRequestValidator();
        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("AB", 5); // 名前が短すぎる (追加バリデータで失敗)

        Task<TestResponse> Next(CancellationToken ct)
        {
            return Task.FromResult(new TestResponse(true));
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await behavior.Handle(request, Next, CancellationToken.None)
        );

        exception.Errors.Should().Contain(e =>
            e.PropertyName == "Name" && e.ErrorMessage.Contains("at least 3 characters"));
    }

    /// <summary>
    /// 追加のテスト用バリデータ
    /// </summary>
    private class AdditionalTestRequestValidator : AbstractValidator<TestRequest>
    {
        public AdditionalTestRequestValidator()
        {
            RuleFor(x => x.Name)
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters");
        }
    }

    /// <summary>
    /// キャンセルトークンが正しく伝播されることを検証
    /// </summary>
    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("John", 25);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Task<TestResponse> Next(CancellationToken ct)
        {
            ct.IsCancellationRequested.Should().BeTrue();
            return Task.FromResult(new TestResponse(true));
        }

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await behavior.Handle(request, Next, cts.Token)
        );
    }

    /// <summary>
    /// 1つのフィールドのみが無効な場合、そのエラーのみが含まれることを検証
    /// </summary>
    [Fact]
    public async Task Handle_WithSingleInvalidField_ShouldContainOnlyThatError()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("", 25); // Name のみ無効

        Task<TestResponse> Next(CancellationToken ct)
        {
            return Task.FromResult(new TestResponse(true));
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await behavior.Handle(request, Next, CancellationToken.None)
        );

        exception.Errors.Should().HaveCount(1);
        exception.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}