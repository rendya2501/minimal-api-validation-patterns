namespace MinimalApiValidationPatterns.ExceptionHandling;

/// <summary>
/// リソースが見つからない例外
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"Entity '{resourceName}' with key '{key}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
