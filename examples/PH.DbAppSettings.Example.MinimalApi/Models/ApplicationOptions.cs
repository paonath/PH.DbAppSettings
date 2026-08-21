namespace PH.DbAppSettings.Example.MinimalApi.Models;

public sealed record ApplicationOptions
{
    public string Title { get; init; } = "";
    public string Version { get; init; } = "";
    public string Environment { get; init; } = "";
    public bool EnableSwagger { get; init; }
    public PaginationOptions Pagination { get; init; } = new();
    public SecurityOptions Security { get; init; } = new();
    public EmailOptions Email { get; init; } = new();
    public FeatureOptions Features { get; init; } = new();
}

public sealed record PaginationOptions
{
    public int DefaultPageSize { get; init; } = 25;
    public int MaxPageSize { get; init; } = 100;
}

public sealed record SecurityOptions
{
    public string JwtIssuer { get; init; } = "";
    public string JwtAudience { get; init; } = "";
    public int TokenExpirationMinutes { get; init; } = 60;
    public bool RequireHttps { get; init; } = true;
}

public sealed record EmailOptions
{
    public string SenderName { get; init; } = "";
    public string SenderEmail { get; init; } = "";
    public SmtpOptions Smtp { get; init; } = new();
}

public sealed record SmtpOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = "";
}

public sealed record FeatureOptions
{
    public bool EnableCache { get; init; } = true;
    public int CacheDurationSeconds { get; init; } = 300;
    public bool MaintenanceMode { get; init; }
    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}
