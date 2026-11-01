namespace Core.Api.Models;

public sealed record RegisterRequest(string FullName, string Email, string Password, bool AcceptTerms = true);

public sealed record LoginRequest(string Email, string Password);

public sealed record OpenAccountRequest(string Kind, string? Name = null, long? OpeningCents = null);

public sealed record DepositRequest(long AmountCents);

public sealed record TransferRequest(string FromAccountId, string ToAccountNumber, long AmountCents, string? Memo = null);
