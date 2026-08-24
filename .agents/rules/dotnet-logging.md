---
trigger: model_decision
description: Logging practices and NLog configuration for .NET projects
globs: '**/*.cs'
---

## Logging Framework

- Use `Microsoft.Extensions.Logging` (`ILogger<T>`) as the logging abstraction.
- Use **NLog** as the logging provider when no other provider is configured.
- Inject `ILogger<T>` via constructor injection.

## Log Levels

| Level | Usage |
|-------|-------|
| `Trace` | Method entry/exit with payload details |
| `Debug` | Diagnostic information for development |
| `Information` | Significant business events |
| `Warning` | Unexpected but recoverable conditions |
| `Error` | Exceptions and failures |
| `Critical` | System-level failures requiring immediate attention |

- Avoid excessive logging at `Error`/`Critical` unless necessary.

## Structured Logging

- Use log message templates with named placeholders (not string interpolation).
- Enrich log messages with context: user ID, request ID, method name.
- Use `nameof()` for method references in log messages.
- Use `@` prefix for structured objects: `{@Payload}`.

## Exception Logging

- Log exceptions at `Error` level or higher.
- Include relevant context (user ID, request ID) in exception logs.
- Use the `ILogger` overload that accepts an `Exception` parameter.

## Example

```csharp
public class ExampleService
{
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(ILogger<ExampleService> logger) => _logger = logger;

    public void PerformOperation(MyClass payload)
    {
        _logger.LogTrace("{Method} started - Payload: '{@Payload}'",
            nameof(PerformOperation), payload);
        try
        {
            // Operation logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Method} failed: {Error}",
                nameof(PerformOperation), ex.Message);
            throw;
        }
        _logger.LogTrace("{Method} completed", nameof(PerformOperation));
    }
}
```

## GZipArchiveFile Target (project-specific)

If the project uses a custom NLog target `xsi:type="GZipArchiveFile"` (e.g., defined in a shared/common assembly):
- Writes active log files as plain text (`.log`).
- Compresses rotated archives asynchronously in GZip format.
- **MUST NOT** reconfigure NLog to route application logs to `.tmp/`.
- For temporary log inspection, use shell read/pipe redirection (`cat`, `grep`, pipes) without changing logger targets.

## Performance

- Use asynchronous logging to avoid blocking the main thread.
- Implement log rotation and retention policies.
- Integrate with monitoring/alerting systems.