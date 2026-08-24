namespace PH.DbAppSettings.Example.MinimalApi.Models;

public sealed record SetSettingRequest(string Key, string? Value);

public sealed record ApiResponse<T>(bool Success, string Message, T? Data);
