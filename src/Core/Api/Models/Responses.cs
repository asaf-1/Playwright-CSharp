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

/// <summary>One line of an account's history. Money in is positive, money out negative.</summary>
public sealed record Transaction(
    string Id,
    string AccountId,
    string Kind,
    long AmountCents,
    long BalanceAfterCents,
    string Description,
    string Memo,
    DateTimeOffset CreatedAt);

public sealed record TransactionPage(
    IReadOnlyList<Transaction> Transactions,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

/// <summary>A coin on the market. Prices are in micro-dollars (1,000,000 = $1).</summary>
public sealed record Coin(string Symbol, string Name, long PriceMicros);

public sealed record Market(IReadOnlyList<Coin> Coins);

/// <summary>The body the bank sends with every error.</summary>
public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Errors = null,
    int? RetryAfterSeconds = null);
