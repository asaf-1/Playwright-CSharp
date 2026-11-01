using Core.Api.Models;

namespace Core.Api;

/// <summary>
/// Thrown when the bank answers with an error. Negative tests catch it and
/// check <see cref="Status"/> and <see cref="Code"/> (e.g. 409 INSUFFICIENT_FUNDS).
/// </summary>
public sealed class BankApiException(string method, string path, int status, ApiError? error)
    : Exception($"{method} {path} answered {status} {error?.Code}: {error?.Message}")
{
    public int Status { get; } = status;
    public string? Code { get; } = error?.Code;
    public ApiError? Error { get; } = error;
}
