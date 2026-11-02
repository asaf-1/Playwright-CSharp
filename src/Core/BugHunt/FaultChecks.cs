using System.Globalization;
using System.Text.Json;
using Core.Api;
using Core.Api.Models;
using Core.Data;
using Core.Pages;
using Microsoft.Playwright;
using static Core.BugHunt.Finding;

namespace Core.BugHunt;

/// <summary>
/// A check for each of the 28 faults planted in Playground Bank (the catalogue
/// is the bank's practice.js, shown at /app/practice). Every check is written
/// for practice mode, where all faults are armed at once, so a check never
/// leans on something another fault breaks.
/// </summary>
public static class FaultChecks
{
    public static IReadOnlyList<FaultCheck> All { get; } =
    [
        // --- Money --------------------------------------------------------------
        new("bankNegativeTransfer", "A transfer must be above zero", NegativeTransfer),
        new("bankDoubleSubmit", "Clicking Confirm twice sends one transfer", DoubleSubmit, NeedsBrowser: true),
        new("bankTransferRace", "Two transfers at once can't overdraw the account", TransferRace),
        new("bankStaleBalance", "The bank page shows the new balance after a transfer", StaleBalance, NeedsBrowser: true),
        new("bankDateFilterOffByOne", "A date range includes its last day", DateFilterOffByOne),
        new("bankStatementTotal", "The statement's total adds up its rows", StatementTotal),
        new("bankLoanRounding", "A loan's schedule pays it off to zero", LoanRounding),
        new("bankPayeeIdor", "Nobody can delete another customer's payee", PayeeIdor),
        new("bankMemoNoLimit", "A transfer memo is at most 140 characters", MemoNoLimit),
        new("bankMonthlyRepeatOverflow", "A monthly payment on the 31st still pays in the short month after", MonthlyRepeatOverflow),
        new("bankCardLimitPerCharge", "A card's daily limit counts the whole day", CardLimitPerCharge),

        // --- Between customers --------------------------------------------------
        new("bankNotificationCount", "Marking all read brings the unread count to zero", NotificationCount),
        new("bankRequestDoublePay", "A money request can be paid only once", RequestDoublePay),
        new("bankSupportStatus", "A customer's reply sends the ticket back to support", SupportStatus),

        // --- Sessions and keys ---------------------------------------------------
        new("bankEndOthersKeepsOne", "Log out everywhere else ends every other session", EndOthersKeepsOne),
        new("bankKeyScopeLeak", "A read-only API key can't add a payee", KeyScopeLeak),

        // --- GraphQL ---------------------------------------------------------------
        new("bankGraphqlOwnerLeak", "GraphQL never shows another customer's name or email", GraphqlOwnerLeak),
        new("bankGraphqlErrorDetail", "GraphQL errors carry no stack trace", GraphqlErrorDetail),
        new("bankGraphqlDepth", "GraphQL refuses a query nested too deep", GraphqlDepth),
        new("bankGraphqlKeyScope", "A read-only API key can't charge a card through GraphQL", GraphqlKeyScope),
        new("bankCursorSkip", "Paging the history with cursors returns every row", CursorSkip),

        // --- Crypto ------------------------------------------------------------------
        new("bankCryptoPriceType", "Coin prices are numbers", CryptoPriceType),
        new("bankQuoteExpired", "A quote can't be used after its 15 seconds", QuoteExpired),
        new("bankProfitSign", "Unrealised profit is value minus cost", ProfitSign),
        new("bankFeeHidden", "A quote's total includes the fee", FeeHidden),
        new("bankBadAddress", "A mistyped wallet address is refused", BadAddress),
        new("bankCryptoFloat", "Sending 0.29 of a coin sends exactly 0.29", CryptoFloat),
        new("bankSellLimitInverted", "A sell order waits for its limit price", SellLimitInverted),
    ];

    // --- Money ------------------------------------------------------------------

    private static async Task<Finding> NegativeTransfer(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var r = await a.Api.SendRawAsync("POST", "/api/bank/transfers",
            new TransferRequest(a.Customer.Checking.Id, a.Customer.Savings.Number, -5_000));
        return r.Status is 200 or 201
            ? Fault($"A transfer of -$50.00 was accepted ({r.Status}).")
            : Clean($"A transfer of -$50.00 was refused ({r.Status}).");
    }

    private static async Task<Finding> DoubleSubmit(FaultContext ctx)
    {
        var b = await ctx.NewBrowserCustomerAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs double");
        var transfer = new TransferPage(b.Page);

        // Hold each transfer request for a moment, so a second click lands while the first is in flight.
        await b.Page.RouteAsync("**/api/bank/transfers", async route =>
        {
            if (route.Request.Method == "POST") await Task.Delay(1_500);
            await route.ContinueAsync();
        });

        await transfer.GotoAsync();
        await transfer.FillToOwnAccountAsync(b.Customer.Checking.Id, b.Customer.Savings.Id, "10.00", memo);
        await transfer.Review.ClickAsync();
        await transfer.Confirm.ClickAsync();
        try
        {
            // A disabled button ignores this click, which is the correct behaviour.
            await transfer.Confirm.ClickAsync(new() { Force = true, Timeout = 1_000 });
        }
        catch (PlaywrightException) { /* the button was gone or disabled */ }

        await Assertions.Expect(transfer.Receipt).ToBeVisibleAsync();
        await Task.Delay(2_500); // let a second request, if any, finish

        var sent = (await b.Api.GetTransactionsAsync(b.Customer.Checking.Id)).Transactions
            .Count(t => t.Kind == "transfer_out" && t.Memo == memo);
        return sent > 1
            ? Fault($"Two clicks on Confirm sent {sent} transfers of $10.00.")
            : Clean($"Two clicks on Confirm sent {sent} transfer.");
    }

    private static async Task<Finding> TransferRace(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var amount = a.Customer.Checking.BalanceCents * 6 / 10;
        var body = new TransferRequest(a.Customer.Checking.Id, a.Customer.Savings.Number, amount);

        var answers = await Task.WhenAll(
            a.Api.SendRawAsync("POST", "/api/bank/transfers", body),
            a.Api.SendRawAsync("POST", "/api/bank/transfers", body));
        var accepted = answers.Count(r => r.Status == 201);
        var balance = (await a.Api.GetAccountAsync(a.Customer.Checking.Id)).BalanceCents;

        return accepted == 2
            ? Fault($"Both transfers of {Money.Format(amount)} went through; Checking is now {Money.Format(balance)}.")
            : Clean($"{accepted} of 2 transfers went through ({string.Join(", ", answers.Select(r => r.Status))}); Checking is {Money.Format(balance)}.");
    }

    private static async Task<Finding> StaleBalance(FaultContext ctx)
    {
        var b = await ctx.NewBrowserCustomerAsync();
        var bank = new BankPage(b.Page);
        var transfer = new TransferPage(b.Page);
        var checking = b.Customer.Checking;
        var expected = Money.Format(checking.BalanceCents - 2_500);

        await bank.GotoAsync();
        await Assertions.Expect(bank.AccountBalance(checking.Id)).ToHaveTextAsync(Money.Format(checking.BalanceCents));

        // Move money and come back through the page's own links, not a reload.
        await bank.AccountTransferLink(checking.Id).ClickAsync();
        await transfer.FillToOwnAccountAsync(checking.Id, b.Customer.Savings.Id, "25.00");
        await transfer.Review.ClickAsync();
        await transfer.Confirm.ClickAsync();
        await Assertions.Expect(transfer.Receipt).ToBeVisibleAsync();
        await transfer.Done.ClickAsync();
        await Assertions.Expect(bank.Root).ToBeVisibleAsync();

        try
        {
            await Assertions.Expect(bank.AccountBalance(checking.Id)).ToHaveTextAsync(expected, new() { Timeout = 5_000 });
            return Clean($"After moving $25.00 the bank page shows {expected}.");
        }
        catch (PlaywrightException)
        {
            var shown = await bank.AccountBalance(checking.Id).TextContentAsync();
            return Fault($"After moving $25.00 the bank page still shows {shown}; the balance is {expected}.");
        }
    }

    private static async Task<Finding> DateFilterOffByOne(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var today = Today();
        var r = await a.Api.SendRawAsync("GET", $"/api/bank/accounts/{a.Customer.Checking.Id}/transactions?from={today}&to={today}");
        var total = r.Json.GetProperty("total").GetInt32();
        return total == 0
            ? Fault($"History from {today} to {today} is empty, though the account opened today.")
            : Clean($"History from {today} to {today} has {total} row(s).");
    }

    private static async Task<Finding> StatementTotal(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        await a.Api.TransferAsync(new TransferRequest(a.Customer.Checking.Id, a.Customer.Savings.Number, 1_234, "Pwcs statement"));

        // To tomorrow, so the last day is inside the range even if the date filter is off by one.
        var r = await a.Api.SendRawAsync("GET",
            $"/api/bank/accounts/{a.Customer.Checking.Id}/statement.csv?from={Today()}&to={Today(1)}");
        var statement = Statement.Parse(r.Body);
        if (statement.Rows.Count < 2) throw new CheckSetupException($"The statement has {statement.Rows.Count} row(s); 2 are needed.");

        var sum = statement.Rows.Sum(row => Money.ParsePlain(row.Amount));
        var total = Money.ParsePlain(statement.Total);
        return total != sum
            ? Fault($"The Total row says {statement.Total}; its {statement.Rows.Count} rows add up to {sum / 100m:0.00}.")
            : Clean($"The Total row {statement.Total} matches its {statement.Rows.Count} rows.");
    }

    private static async Task<Finding> LoanRounding(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        // Cutting the payment off instead of rounding it only shows when the
        // payment's fraction of a cent is half or more, which depends on the
        // amount and on this customer's rate. So try up to 20 amounts: the rule
        // must hold for every one, and the fault needs to show in only one.
        for (var i = 0; i < 20; i++)
        {
            var amount = 1_000_000 + i * 137;
            var r = await a.Api.SendRawAsync("GET", $"/api/bank/loans/quote?amountCents={amount}&termMonths=12");
            var owed = ReadLong(r.Json.GetProperty("schedule").EnumerateArray().Last().GetProperty("balanceCents"));
            if (owed != 0)
            {
                return Fault($"A {Money.Format(amount)} loan still owes {Money.Format(owed)} after its last of 12 payments.");
            }
        }
        return Clean("20 loan quotes, from $10,000.00 up, each end at zero after 12 payments.");
    }

    private static async Task<Finding> PayeeIdor(FaultContext ctx)
    {
        var attacker = await ctx.NewCustomerAsync();
        var victim = await ctx.NewCustomerAsync();
        var payee = await victim.Api.AddPayeeAsync("Pwcs Victim Payee", "VIC-1");

        var r = await attacker.Api.SendRawAsync("DELETE", $"/api/bank/payees/{payee.Id}", new { });
        var stillThere = (await victim.Api.GetPayeesAsync()).Any(p => p.Id == payee.Id);

        return r.Status == 200 || !stillThere
            ? Fault($"Another customer deleted the payee ({r.Status}); still there for its owner: {stillThere}.")
            : Clean($"Another customer's delete was refused ({r.Status}); the payee is still there.");
    }

    private static async Task<Finding> MemoNoLimit(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var r = await a.Api.SendRawAsync("POST", "/api/bank/transfers",
            new TransferRequest(a.Customer.Checking.Id, a.Customer.Savings.Number, 100, new string('x', 5_000)));
        return r.Status is 200 or 201
            ? Fault($"A 5,000-character memo was stored ({r.Status}).")
            : Clean($"A 5,000-character memo was refused ({r.Status}).");
    }

    private static async Task<Finding> MonthlyRepeatOverflow(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var today = DateOnly.Parse((await a.Api.SendRawAsync("GET", "/api/bank/clock")).Json.GetProperty("today").GetString()!, CultureInfo.InvariantCulture);

        // The next 31st whose following month is shorter, e.g. January 31.
        var due = today;
        while (!(due.Day == 31 && DateTime.DaysInMonth(due.AddMonths(1).Year, due.AddMonths(1).Month) < 31)) due = due.AddDays(1);
        var nextMonth = due.AddMonths(1);
        var expected = new DateOnly(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));

        var payee = await a.Api.AddPayeeAsync("Pwcs Rent", "RENT-31");
        var created = await a.Api.SendRawAsync("POST", "/api/bank/scheduled-payments", new
        {
            fromAccountId = a.Customer.Checking.Id,
            payeeId = payee.Id,
            amountCents = 100,
            repeat = "monthly",
            firstDue = due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        });
        if (created.Status != 201) throw new CheckSetupException($"Scheduling the payment answered {created.Status}: {created.Body}");
        var id = created.Json.GetProperty("payment").GetProperty("id").GetString();

        // Move this customer's own clock 3 days past the 31st, so the first payment
        // runs even when the 31st is a weekend (the bank pays on the next Monday).
        await a.Api.SendRawAsync("POST", "/api/bank/clock/advance", new { days = due.DayNumber - today.DayNumber + 3 });

        var payment = (await a.Api.SendRawAsync("GET", "/api/bank/scheduled-payments")).Json.GetProperty("payments")
            .EnumerateArray().Single(p => p.GetProperty("id").GetString() == id);
        if (payment.GetProperty("runs").GetInt32() < 1) throw new CheckSetupException("The first payment didn't run.");
        var nextDue = payment.GetProperty("nextDue").GetString();
        var wanted = expected.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return nextDue != wanted
            ? Fault($"After paying on {due:yyyy-MM-dd} the next payment is {nextDue}; it should be {wanted}.")
            : Clean($"After paying on {due:yyyy-MM-dd} the next payment is {nextDue}.");
    }

    private static async Task<Finding> CardLimitPerCharge(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var card = await a.Api.IssueCardAsync(a.Customer.Checking.Id);
        var charge = card.LimitCents * 6 / 10;

        var first = await a.Api.SendRawAsync("POST", $"/api/bank/cards/{card.Id}/charges", new { merchant = "Pwcs Shop", amountCents = charge });
        var second = await a.Api.SendRawAsync("POST", $"/api/bank/cards/{card.Id}/charges", new { merchant = "Pwcs Shop", amountCents = charge });
        string Status(ApiResponse r) => r.Json.GetProperty("charge").GetProperty("status").GetString()!;

        if (Status(first) != "approved") throw new CheckSetupException($"The first charge was {Status(first)}.");
        return Status(second) == "approved"
            ? Fault($"Two charges of {Money.Format(charge)} were approved on a daily limit of {Money.Format(card.LimitCents)}.")
            : Clean($"The second charge of {Money.Format(charge)} was {Status(second)} (limit {Money.Format(card.LimitCents)}).");
    }

    // --- Between customers --------------------------------------------------------

    private static async Task<Finding> NotificationCount(FaultContext ctx)
    {
        var asker = await ctx.NewCustomerAsync();
        var payer = await ctx.NewCustomerAsync();
        await AskForMoney(asker, payer, 500);

        var before = (await payer.Api.SendRawAsync("GET", "/api/bank/notifications")).Json.GetProperty("unread").GetInt32();
        if (before == 0) throw new CheckSetupException("The money request brought no notification.");

        await payer.Api.SendRawAsync("POST", "/api/bank/notifications/read-all", new { });
        var after = (await payer.Api.SendRawAsync("GET", "/api/bank/notifications")).Json.GetProperty("unread").GetInt32();

        return after > 0
            ? Fault($"After marking all read, the unread count is {after} (was {before}).")
            : Clean($"After marking all read, the unread count is 0 (was {before}).");
    }

    private static async Task<Finding> RequestDoublePay(FaultContext ctx)
    {
        var asker = await ctx.NewCustomerAsync();
        var payer = await ctx.NewCustomerAsync();
        var requestId = await AskForMoney(asker, payer, 500);
        var pay = new { fromAccountId = payer.Customer.Checking.Id };

        var first = await payer.Api.SendRawAsync("POST", $"/api/bank/requests/{requestId}/pay", pay);
        if (first.Status is not (200 or 201)) throw new CheckSetupException($"Paying the request answered {first.Status}.");
        var second = await payer.Api.SendRawAsync("POST", $"/api/bank/requests/{requestId}/pay", pay);

        return second.Status is 200 or 201
            ? Fault($"The same request was paid a second time ({second.Status}).")
            : Clean($"Paying the request again was refused ({second.Status}).");
    }

    private static async Task<Finding> SupportStatus(FaultContext ctx)
    {
        var customer = await ctx.NewCustomerAsync();
        var ticket = await customer.Api.SendRawAsync("POST", "/api/bank/support", new { subject = "Pwcs bug hunt", body = "Pwcs test ticket." });
        var ticketId = ticket.Json.GetProperty("ticket").GetProperty("id").GetString();

        var staff = await ctx.SignInAsync(DemoAccounts.Support, DemoAccounts.Password);
        var answered = await staff.SendRawAsync("POST", $"/api/bank/support/{ticketId}/messages", new { body = "Pwcs staff reply." });
        if (TicketStatus(answered) != "answered") throw new CheckSetupException($"The staff reply left the ticket {TicketStatus(answered)}.");

        var reply = await customer.Api.SendRawAsync("POST", $"/api/bank/support/{ticketId}/messages", new { body = "Pwcs customer reply." });
        var status = TicketStatus(reply);
        return status != "open"
            ? Fault($"After the customer replied, the ticket stayed \"{status}\", so support never sees it.")
            : Clean("After the customer replied, the ticket went back to \"open\".");

        static string? TicketStatus(ApiResponse r) => r.Json.GetProperty("ticket").GetProperty("status").GetString();
    }

    // --- Sessions and keys --------------------------------------------------------

    private static async Task<Finding> EndOthersKeepsOne(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var second = await ctx.SignInAsync(a.Customer.Email, a.Customer.Password);
        var third = await ctx.SignInAsync(a.Customer.Email, a.Customer.Password);

        await a.Api.SendRawAsync("POST", "/api/bank/sessions/end-others", new { });
        var stillIn = 0;
        foreach (var device in new[] { second, third })
        {
            if ((await device.SendRawAsync("GET", "/api/bank/me")).Status == 200) stillIn++;
        }

        return stillIn > 0
            ? Fault($"After \"log out everywhere else\", {stillIn} of 2 other devices are still signed in.")
            : Clean("After \"log out everywhere else\", both other devices are signed out.");
    }

    private static async Task<Finding> KeyScopeLeak(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var token = await NewReadKey(a);
        var keyClient = await ctx.NewClientAsync();

        var r = await keyClient.SendRawAsync("POST", "/api/bank/payees" + a.RunKeyQuery, new PayeeRequest("Pwcs Key Payee", "KEY-1"),
            headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" });
        return r.Status is 200 or 201
            ? Fault($"A read-only key added a payee ({r.Status}).")
            : Clean($"A read-only key was refused ({r.Status}).");
    }

    // --- GraphQL -----------------------------------------------------------------

    private static async Task<Finding> GraphqlOwnerLeak(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var b = await ctx.NewCustomerAsync();
        var memo = FreshCustomer.UniqueMemo("Pwcs leak");
        await a.Api.TransferAsync(new TransferRequest(a.Customer.Checking.Id, b.Customer.Checking.Number, 100, memo));

        var reply = await a.Api.GraphqlAsync(
            "query ($id: ID!) { account(id: $id) { transactions { transactions { memo counterpartyDetails { accountNumber fullName email } } } } }",
            new { id = a.Customer.Checking.Id });
        var row = reply.Data!.Value.GetProperty("account").GetProperty("transactions").GetProperty("transactions")
            .EnumerateArray().Single(t => t.GetProperty("memo").GetString() == memo);
        var details = row.GetProperty("counterpartyDetails");
        var name = details.GetProperty("fullName");
        var email = details.GetProperty("email");

        return name.ValueKind != JsonValueKind.Null || email.ValueKind != JsonValueKind.Null
            ? Fault($"The other customer's details came back: {name} / {email}.")
            : Clean("Only the other account's number came back; name and email were null.");
    }

    private static async Task<Finding> GraphqlErrorDetail(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        // Known errors ("no such account") are always answered cleanly; only an
        // unexpected one shows the fault. A null byte in the memo is refused by
        // the database itself, below the bank's own checks (the bank's own test
        // uses the same trick). The transfer is rolled back; nothing is stored.
        var r = await a.Api.SendRawAsync("POST", "/api/bank/graphql", new
        {
            query = "mutation ($from: ID!, $to: String!, $memo: String) { transfer(fromAccountId: $from, toAccountNumber: $to, amountCents: 100, memo: $memo) { replayed } }",
            variables = new { from = a.Customer.Checking.Id, to = a.Customer.Savings.Number, memo = "bad\u0000memo" },
        });
        if (!r.Body.Contains("\"errors\"")) throw new CheckSetupException($"The bad memo brought no error: {Trim(r.Body)}");

        var leaks = r.Body.Contains("stacktrace", StringComparison.OrdinalIgnoreCase) || r.Body.Contains("node_modules");
        return leaks
            ? Fault($"The error carries the server's message and stack trace: {Trim(r.Body)}")
            : Clean($"The error is generic: {Trim(r.Body)}");
    }

    private static async Task<Finding> GraphqlDepth(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        // Ten levels: account > transactions > transactions > transfer > fromAccount > ... > id.
        const string deep = "query ($id: ID!) { account(id: $id) { transactions { transactions { transfer { fromAccount { " +
                            "transactions { transactions { transfer { fromAccount { id } } } } } } } } } }";
        var reply = await a.Api.GraphqlAsync(deep, new { id = a.Customer.Checking.Id });
        return !reply.HasErrors
            ? Fault("A query ten levels deep was run.")
            : Clean($"A query ten levels deep was refused: {reply.Errors![0].Message}");
    }

    private static async Task<Finding> GraphqlKeyScope(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var card = await a.Api.IssueCardAsync(a.Customer.Checking.Id);
        var token = await NewReadKey(a);
        var keyClient = await ctx.NewClientAsync();

        var r = await keyClient.SendRawAsync("POST", "/api/bank/graphql" + a.RunKeyQuery,
            new { query = $"mutation {{ chargeCard(id: \"{card.Id}\", merchant: \"Pwcs Shop\", amountCents: 100) {{ charge {{ status }} }} }}" },
            headers: new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" });
        var charged = r.Status == 200
            && r.Json.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("chargeCard", out var charge) && charge.ValueKind == JsonValueKind.Object;

        return charged
            ? Fault("A read-only key charged the card through GraphQL.")
            : Clean($"A read-only key's charge was refused: {Trim(r.Body)}");
    }

    private static async Task<Finding> CursorSkip(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        for (var i = 0; i < 4; i++) await a.Api.DepositAsync(a.Customer.Checking.Id, 100);

        var seen = new List<string>();
        string? after = null;
        var total = 0;
        for (var page = 0; page < 10; page++)
        {
            var reply = await a.Api.GraphqlAsync(
                "query ($id: ID!, $after: String) { account(id: $id) { history(first: 2, after: $after) { nodes { id } pageInfo { endCursor hasNextPage } total } } }",
                new { id = a.Customer.Checking.Id, after });
            var history = reply.Data!.Value.GetProperty("account").GetProperty("history");
            total = history.GetProperty("total").GetInt32();
            seen.AddRange(history.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("id").GetString()!));
            if (!history.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean()) break;
            after = history.GetProperty("pageInfo").GetProperty("endCursor").GetString();
        }

        var distinct = seen.Distinct().Count();
        return distinct < total
            ? Fault($"Paging two at a time returned {distinct} of {total} rows.")
            : Clean($"Paging two at a time returned all {total} rows.");
    }

    // --- Crypto ------------------------------------------------------------------

    private static async Task<Finding> CryptoPriceType(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var price = (await a.Api.SendRawAsync("GET", "/api/bank/market")).Json.GetProperty("coins")[0].GetProperty("priceMicros");
        return price.ValueKind == JsonValueKind.String
            ? Fault($"priceMicros came back as text: {price.GetRawText()}.")
            : Clean($"priceMicros came back as a number: {price.GetRawText()}.");
    }

    private static async Task<Finding> QuoteExpired(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var symbol = await CheapestCoin(a);
        var quote = await a.Api.SendRawAsync("POST", "/api/bank/trades/quote", new { symbol, side = "buy", spendCents = 1_000 });
        var quoteId = quote.Json.GetProperty("id").GetString();

        await Task.Delay(TimeSpan.FromSeconds(16)); // the quote holds for 15 seconds
        var trade = await a.Api.SendRawAsync("POST", "/api/bank/trades", new { quoteId, accountId = a.Customer.Checking.Id });

        return trade.Status == 201
            ? Fault("A quote was used 16 seconds after it was given, past its 15 seconds.")
            : Clean($"A 16-second-old quote was refused ({trade.Status}).");
    }

    private static async Task<Finding> ProfitSign(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        await Buy(a, await CheapestCoin(a), 50_000);

        var portfolio = (await a.Api.SendRawAsync("GET", "/api/bank/portfolio")).Json;
        var value = ReadLong(portfolio.GetProperty("valueCents"));
        var cost = ReadLong(portfolio.GetProperty("costCents"));
        var unrealised = ReadLong(portfolio.GetProperty("unrealisedCents"));
        if (value == cost) throw new CheckSetupException("Value equals cost, so a flipped sign can't show.");

        return unrealised != value - cost
            ? Fault($"Unrealised is {Money.Format(unrealised)}, but value {Money.Format(value)} minus cost {Money.Format(cost)} is {Money.Format(value - cost)}.")
            : Clean($"Unrealised {Money.Format(unrealised)} is value minus cost.");
    }

    private static async Task<Finding> FeeHidden(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var quote = (await a.Api.SendRawAsync("POST", "/api/bank/trades/quote",
            new { symbol = await CheapestCoin(a), side = "buy", spendCents = 50_000 })).Json;
        var gross = ReadLong(quote.GetProperty("grossCents"));
        var fee = ReadLong(quote.GetProperty("feeCents"));
        var total = ReadLong(quote.GetProperty("netCents"));

        return total != gross + fee
            ? Fault($"The quote's total is {Money.Format(total)}; the coins {Money.Format(gross)} plus the fee {Money.Format(fee)} is {Money.Format(gross + fee)}.")
            : Clean($"The quote's total {Money.Format(total)} includes the fee {Money.Format(fee)}.");
    }

    private static async Task<Finding> BadAddress(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var b = await ctx.NewCustomerAsync();
        var symbol = await CheapestCoin(a);
        await Buy(a, symbol, 50_000);

        var address = await WalletAddress(b, symbol);
        var mistyped = address[..^1] + (address.EndsWith('0') ? "1" : "0");
        var r = await a.Api.SendRawAsync("POST", "/api/bank/wallet/send", new { symbol, toAddress = mistyped, quantity = "0.01" });

        return r.Status == 201
            ? Fault($"A send to the mistyped address {mistyped} was accepted, and the coin left the wallet.")
            : Clean($"The mistyped address {mistyped} was refused ({r.Status}).");
    }

    private static async Task<Finding> CryptoFloat(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var symbol = await CheapestCoin(a);
        await Buy(a, symbol, 50_000);

        var preview = (await a.Api.SendRawAsync("POST", "/api/bank/wallet/send/preview", new { symbol, quantity = "0.29" })).Json;
        var atoms = ReadLong(preview.GetProperty("quantityAtoms"));
        const long expected = 29_000_000; // 0.29 with 8 decimals

        return atoms != expected
            ? Fault($"Sending 0.29 {symbol} would send {atoms} atoms; 0.29 is {expected}.")
            : Clean($"Sending 0.29 {symbol} sends exactly {atoms} atoms.");
    }

    private static async Task<Finding> SellLimitInverted(FaultContext ctx)
    {
        var a = await ctx.NewCustomerAsync();
        var symbol = await CheapestCoin(a);
        var bought = await Buy(a, symbol, 50_000);
        var price = await CoinPrice(a, symbol);

        var order = await a.Api.SendRawAsync("POST", "/api/bank/orders", new
        {
            symbol,
            side = "sell",
            accountId = a.Customer.Checking.Id,
            quantityAtoms = bought / 2,
            limitMicros = price * 2,
        });
        if (order.Status != 201) throw new CheckSetupException($"Placing the order answered {order.Status}: {order.Body}");
        var orderId = order.Json.GetProperty("order").GetProperty("id").GetString();

        var status = (await a.Api.SendRawAsync("GET", "/api/bank/orders")).Json.GetProperty("orders").EnumerateArray()
            .Single(o => o.GetProperty("id").GetString() == orderId).GetProperty("status").GetString();
        return status == "filled"
            ? Fault("A sell order with a limit of twice the price filled at once, below its limit.")
            : Clean($"A sell order with a limit of twice the price is {status}, waiting for its price.");
    }

    // --- Helpers -----------------------------------------------------------------

    private static string Today(int addDays = 0) =>
        DateTime.UtcNow.Date.AddDays(addDays).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Reads a number the bank may send as a number or, with a fault armed, as text.</summary>
    private static long ReadLong(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? long.Parse(value.GetString()!, CultureInfo.InvariantCulture) : value.GetInt64();

    private static string Trim(string text) => text.Length <= 160 ? text : text[..160] + "…";

    private static async Task<string> AskForMoney(Party asker, Party payer, long amountCents)
    {
        var r = await asker.Api.SendRawAsync("POST", "/api/bank/requests", new
        {
            toAccountId = asker.Customer.Checking.Id,
            fromAccountNumber = payer.Customer.Checking.Number,
            amountCents,
            memo = "Pwcs bug hunt",
        });
        if (r.Status != 201) throw new CheckSetupException($"Asking for money answered {r.Status}: {r.Body}");
        return r.Json.GetProperty("request").GetProperty("id").GetString()!;
    }

    private static async Task<string> NewReadKey(Party party)
    {
        var r = await party.Api.SendRawAsync("POST", "/api/bank/keys", new { name = "Pwcs read key", scope = "read" });
        if (r.Status != 201) throw new CheckSetupException($"Making a key answered {r.Status}: {r.Body}");
        return r.Json.GetProperty("token").GetString()!;
    }

    private static async Task<string> CheapestCoin(Party party) =>
        (await party.Api.SendRawAsync("GET", "/api/bank/market")).Json.GetProperty("coins").EnumerateArray()
            .MinBy(c => ReadLong(c.GetProperty("priceMicros")))
            .GetProperty("symbol").GetString()!;

    private static async Task<long> CoinPrice(Party party, string symbol) =>
        ReadLong((await party.Api.SendRawAsync("GET", "/api/bank/market")).Json.GetProperty("coins").EnumerateArray()
            .Single(c => c.GetProperty("symbol").GetString() == symbol).GetProperty("priceMicros"));

    /// <summary>Buys a coin for this many cents from Checking; returns the atoms bought.</summary>
    private static async Task<long> Buy(Party party, string symbol, long spendCents)
    {
        var quote = await party.Api.SendRawAsync("POST", "/api/bank/trades/quote", new { symbol, side = "buy", spendCents });
        if (quote.Status != 200) throw new CheckSetupException($"The buy quote answered {quote.Status}: {quote.Body}");
        var trade = await party.Api.SendRawAsync("POST", "/api/bank/trades",
            new { quoteId = quote.Json.GetProperty("id").GetString(), accountId = party.Customer.Checking.Id });
        if (trade.Status != 201) throw new CheckSetupException($"The buy answered {trade.Status}: {trade.Body}");
        return ReadLong(trade.Json.GetProperty("trade").GetProperty("quantityAtoms"));
    }

    private static async Task<string> WalletAddress(Party party, string symbol) =>
        (await party.Api.SendRawAsync("GET", "/api/bank/wallet")).Json.GetProperty("wallets").EnumerateArray()
            .Single(w => w.GetProperty("symbol").GetString() == symbol).GetProperty("address").GetString()!;
}
