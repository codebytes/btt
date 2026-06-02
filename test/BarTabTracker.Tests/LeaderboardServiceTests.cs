using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Leaderboard;
using BarTabTracker.Server.Storage;

namespace BarTabTracker.Tests;

public sealed class LeaderboardServiceTests
{
    [Fact]
    public async Task Orders_by_tab_count_descending()
    {
        var tabs = new FakeTabRepository(
            new OwnerTabCount("u1", 3),
            new OwnerTabCount("u2", 7),
            new OwnerTabCount("u3", 5));
        var users = new FakeUserRepository(
            User("u1", "Alice"),
            User("u2", "Bob"),
            User("u3", "Carol"));

        var result = await new LeaderboardService(tabs, users).GetLeaderboard(null);

        Assert.Equal(["u2", "u3", "u1"], result.Select(entry => entry.UserId));
        Assert.Equal([7, 5, 3], result.Select(entry => entry.TabCount));
    }

    [Fact]
    public async Task Breaks_ties_by_display_name_ascending()
    {
        var tabs = new FakeTabRepository(
            new OwnerTabCount("u1", 4),
            new OwnerTabCount("u2", 4),
            new OwnerTabCount("u3", 4));
        var users = new FakeUserRepository(
            User("u1", "Charlie"),
            User("u2", "Alice"),
            User("u3", "Bob"));

        var result = await new LeaderboardService(tabs, users).GetLeaderboard(null);

        Assert.Equal(["Alice", "Bob", "Charlie"], result.Select(entry => entry.DisplayName));
        Assert.Equal(["u2", "u3", "u1"], result.Select(entry => entry.UserId));
    }

    [Fact]
    public async Task Resolves_display_name_and_avatar_from_user_repository()
    {
        var tabs = new FakeTabRepository(new OwnerTabCount("u1", 1));
        var users = new FakeUserRepository(User("u1", "Alice", "https://example.test/a.png"));

        var result = await new LeaderboardService(tabs, users).GetLeaderboard(null);

        var entry = Assert.Single(result);
        Assert.Equal("Alice", entry.DisplayName);
        Assert.Equal("https://example.test/a.png", entry.AvatarUrl);
        Assert.Equal(1, entry.TabCount);
    }

    [Fact]
    public async Task Falls_back_to_owner_id_when_user_is_missing()
    {
        var tabs = new FakeTabRepository(new OwnerTabCount("ghost", 2));
        var users = new FakeUserRepository();

        var result = await new LeaderboardService(tabs, users).GetLeaderboard(null);

        var entry = Assert.Single(result);
        Assert.Equal("ghost", entry.DisplayName);
        Assert.Null(entry.AvatarUrl);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(25, 25)]
    [InlineData(50, 50)]
    [InlineData(51, 50)]
    [InlineData(1000, 50)]
    public void Clamps_limit_to_valid_range(int? requested, int expected) =>
        Assert.Equal(expected, LeaderboardService.ClampLimit(requested));

    [Fact]
    public async Task Default_limit_returns_at_most_ten_entries()
    {
        var counts = Enumerable.Range(0, 20)
            .Select(index => new OwnerTabCount($"u{index:00}", 20 - index))
            .ToArray();
        var tabs = new FakeTabRepository(counts);
        var users = new FakeUserRepository();

        var result = await new LeaderboardService(tabs, users).GetLeaderboard(null);

        Assert.Equal(10, result.Count);
        Assert.Equal(20, result[0].TabCount);
        Assert.Equal(11, result[^1].TabCount);
    }

    [Fact]
    public async Task Limit_is_clamped_before_taking_entries()
    {
        var counts = Enumerable.Range(0, 5)
            .Select(index => new OwnerTabCount($"u{index}", 5 - index))
            .ToArray();
        var tabs = new FakeTabRepository(counts);
        var users = new FakeUserRepository();

        var unclamped = await new LeaderboardService(tabs, users).GetLeaderboard(0);
        var capped = await new LeaderboardService(tabs, users).GetLeaderboard(100);

        Assert.Single(unclamped);
        Assert.Equal(5, capped.Count);
    }

    private static User User(string id, string displayName, string? avatarUrl = null) => new()
    {
        Id = id,
        OAuthSubject = $"oauth:{id}",
        DisplayName = displayName,
        AvatarUrl = avatarUrl,
        CreatedAt = DateTimeOffset.UnixEpoch
    };

    private sealed class FakeTabRepository(params OwnerTabCount[] counts) : ITabRepository
    {
        private readonly IReadOnlyList<OwnerTabCount> counts = counts;

        public Task<IReadOnlyList<OwnerTabCount>> GetOwnerTabCounts() =>
            Task.FromResult(counts);

        public Task<Tab?> GetById(string id) => throw new NotSupportedException();

        public Task<Tab?> GetByInviteToken(string inviteToken) => throw new NotSupportedException();

        public Task Create(Tab tab) => throw new NotSupportedException();

        public Task Update(Tab tab) => throw new NotSupportedException();

        public Task Delete(string id) => throw new NotSupportedException();

        public Task<IReadOnlyList<TabItem>> GetItems(string tabId) => throw new NotSupportedException();

        public Task AddItem(TabItem item) => throw new NotSupportedException();

        public Task RemoveItem(string tabId, string itemId) => throw new NotSupportedException();

        public Task<IReadOnlyList<Tab>> GetTabsForUser(string userId) => throw new NotSupportedException();

        public Task AddMember(string tabId, string userId) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        private readonly Dictionary<string, User> usersById =
            users.ToDictionary(user => user.Id, StringComparer.Ordinal);

        public Task<User?> GetById(string id) =>
            Task.FromResult(usersById.TryGetValue(id, out var user) ? user : null);

        public Task<User?> GetByOAuthSubject(string oauthSubject) => throw new NotSupportedException();

        public Task Upsert(User user) => throw new NotSupportedException();
    }
}
