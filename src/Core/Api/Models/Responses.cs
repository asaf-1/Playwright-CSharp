namespace Core.Api.Models;

public sealed record BankUser(
    string Id,
    string Email,
    string FullName,
    string Role,
    string Status,
    bool IsDemo,
    DateTimeOffset CreatedAt);

public sealed record Session(BankUser User);

public sealed record BankAccount(
    string Id,
    string Number,
    string Kind,
    string Name,
    long BalanceCents,
    DateTimeOffset CreatedAt);

public sealed record AccountList(IReadOnlyList<BankAccount> Accounts, long TotalCents);

public sealed record AccountReply(BankAccount Account);

public sealed record Transfer(
    string Id,
    string FromAccountId,
    string ToAccountNumber,
    long AmountCents,
    string Memo,
    DateTimeOffset CreatedAt);

public sealed record TransferReply(Transfer Transfer, BankAccount FromAccount, bool Replayed);

/// <summary>The body the bank sends with every error.</summary>
public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Errors = null,
    int? RetryAfterSeconds = null);
