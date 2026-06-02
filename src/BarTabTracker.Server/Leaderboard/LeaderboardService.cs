using BarTabTracker.Server.Storage;

namespace BarTabTracker.Server.Leaderboard;

public sealed class LeaderboardService(ITabRepository tabs, IUserRepository users)
{
    public const int DefaultLimit = 10;
    public const int MinLimit = 1;
    public const int MaxLimit = 50;

    public static int ClampLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, MinLimit, MaxLimit);

    public async Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboard(int? limit)
    {
        var max = ClampLimit(limit);
        var counts = await tabs.GetOwnerTabCounts();

        var entries = new List<LeaderboardEntry>(counts.Count);
        foreach (var count in counts)
        {
            var user = await users.GetById(count.OwnerId);
            entries.Add(new LeaderboardEntry(
                count.OwnerId,
                user?.DisplayName ?? count.OwnerId,
                user?.AvatarUrl,
                count.TabCount));
        }

        return entries
            .OrderByDescending(entry => entry.TabCount)
            .ThenBy(entry => entry.DisplayName, StringComparer.Ordinal)
            .Take(max)
            .ToArray();
    }
}

public sealed record LeaderboardEntry(string UserId, string DisplayName, string? AvatarUrl, int TabCount);
