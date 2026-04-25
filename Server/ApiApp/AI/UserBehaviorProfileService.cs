using System.Text.Json;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.AI;

public record UserBehaviorContext(
    decimal AvgTransferAmount,
    decimal AvgDailySpend,
    decimal MonthlyBudget,
    IReadOnlyList<string> CommonTransferTo,
    IReadOnlyList<string> CommonReceiveFrom,
    IReadOnlyList<string> CommonCategories,
    int? TypicalTransferHour,
    string RiskTolerance,
    int RiskScoreBaseline
);

public class UserBehaviorProfileService
{
    private const int MaxCommonItems = 12;
    private readonly AppDbContext _db;

    public UserBehaviorProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserBehaviorContext?> GetContextAsync(Guid? userId, CancellationToken ct)
    {
        if (userId is null)
            return null;

        var profile = await _db.UserBehaviorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);

        if (profile is null)
            return null;

        return new UserBehaviorContext(
            profile.AvgTransferAmount,
            profile.AvgDailySpend,
            profile.MonthlyBudget,
            ReadList(profile.CommonTransferToJson),
            ReadList(profile.CommonReceiveFromJson),
            ReadList(profile.CommonCategoriesJson),
            profile.TypicalTransferHour,
            profile.RiskTolerance,
            profile.RiskScoreBaseline
        );
    }

    public async Task UpdateAfterTransactionAsync(
        Guid? userId,
        decimal amount,
        string transferToKey,
        string category,
        DateTime timestampUtc,
        bool isTransfer,
        CancellationToken ct)
    {
        if (userId is null || amount <= 0)
            return;

        var profile = await _db.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);

        if (profile is null)
        {
            profile = new UserBehaviorProfile { UserId = userId.Value };
            _db.UserBehaviorProfiles.Add(profile);
        }

        profile.SpendCount += 1;
        profile.AvgDailySpend = RollingAverage(profile.AvgDailySpend, amount, profile.SpendCount);

        if (isTransfer)
        {
            profile.TransferCount += 1;
            profile.AvgTransferAmount = RollingAverage(profile.AvgTransferAmount, amount, profile.TransferCount);
            profile.TypicalTransferHour = UpdateTypicalHour(profile.TypicalTransferHour, timestampUtc.Hour, profile.TransferCount);
        }

        profile.CommonTransferToJson = WriteList(AddMostRecent(ReadList(profile.CommonTransferToJson), transferToKey));
        profile.CommonCategoriesJson = WriteList(AddMostRecent(ReadList(profile.CommonCategoriesJson), category));
        profile.MonthlyBudget = await GetCurrentMonthlyBudgetAsync(userId.Value, timestampUtc, ct);
        profile.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAfterIncomingTransferAsync(
        Guid? userId,
        string transferFromKey,
        DateTime timestampUtc,
        CancellationToken ct)
    {
        if (userId is null)
            return;

        var profile = await _db.UserBehaviorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, ct);

        if (profile is null)
        {
            profile = new UserBehaviorProfile { UserId = userId.Value };
            _db.UserBehaviorProfiles.Add(profile);
        }

        profile.CommonReceiveFromJson = WriteList(AddMostRecent(ReadList(profile.CommonReceiveFromJson), transferFromKey));
        profile.MonthlyBudget = await GetCurrentMonthlyBudgetAsync(userId.Value, timestampUtc, ct);
        profile.LastUpdated = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> RebuildAllProfilesFromTransactionsAsync(CancellationToken ct)
    {
        await _db.UserBehaviorProfiles.ExecuteDeleteAsync(ct);

        var walletOwners = await _db.Wallets
            .AsNoTracking()
            .Where(w => w.user_id != null)
            .Select(w => new { w.wallet_id, UserId = w.user_id!.Value })
            .ToDictionaryAsync(w => w.wallet_id, w => w.UserId, ct);

        var transactions = await _db.Transactions
            .AsNoTracking()
            .Where(t =>
                t.from_wallet_id != null &&
                t.to_wallet_id != null &&
                (t.transaction_type == "pay" || t.transaction_type == "transfer"))
            .OrderBy(t => t.transaction_timestamp)
            .Select(t => new
            {
                t.from_wallet_id,
                t.to_wallet_id,
                t.transaction_amount,
                t.category,
                t.transaction_timestamp,
                t.transaction_type
            })
            .ToListAsync(ct);

        foreach (var transaction in transactions)
        {
            var fromWalletId = transaction.from_wallet_id!.Value;
            var toWalletId = transaction.to_wallet_id!.Value;
            var category = string.IsNullOrWhiteSpace(transaction.category)
                ? Category.Other.ToString()
                : transaction.category;

            walletOwners.TryGetValue(fromWalletId, out var fromUserId);
            walletOwners.TryGetValue(toWalletId, out var toUserId);

            await UpdateAfterTransactionAsync(
                fromUserId == Guid.Empty ? null : fromUserId,
                transaction.transaction_amount,
                toWalletId.ToString(),
                category,
                transaction.transaction_timestamp,
                true,
                ct);

            await UpdateAfterIncomingTransferAsync(
                toUserId == Guid.Empty ? null : toUserId,
                fromWalletId.ToString(),
                transaction.transaction_timestamp,
                ct);
        }

        return await _db.UserBehaviorProfiles.CountAsync(ct);
    }

    private static decimal RollingAverage(decimal currentAverage, decimal nextValue, int count)
        => count <= 1 ? nextValue : Math.Round(currentAverage + ((nextValue - currentAverage) / count), 2);

    private static int UpdateTypicalHour(int? currentHour, int nextHour, int count)
        => count <= 1 || currentHour is null
            ? nextHour
            : (int)Math.Round(currentHour.Value + ((nextHour - currentHour.Value) / (double)count));

    private async Task<decimal> GetCurrentMonthlyBudgetAsync(Guid userId, DateTime timestampUtc, CancellationToken ct)
    {
        var monthStart = new DateTime(timestampUtc.Year, timestampUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = monthStart.AddMonths(1);

        return await _db.Budgets
            .AsNoTracking()
            .Where(b =>
                b.UserId == userId &&
                b.CycleStart < nextMonth &&
                b.CycleEnd > monthStart)
            .SumAsync(b => b.LimitAmount, ct);
    }

    private static List<string> ReadList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string WriteList(List<string> values)
        => JsonSerializer.Serialize(values);

    private static List<string> AddMostRecent(List<string> values, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return values.Take(MaxCommonItems).ToList();

        values.RemoveAll(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
        values.Insert(0, value);
        return values.Take(MaxCommonItems).ToList();
    }
}
