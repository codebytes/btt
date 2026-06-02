using System.Text.Json;
using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Storage;
using StackExchange.Redis;

namespace BarTabTracker.Server.Storage.Redis;

public sealed class RedisTabRepository(IConnectionMultiplexer connection) : ITabRepository
{
    private readonly IDatabase database = connection.GetDatabase();

    public async Task<Tab?> GetById(string id)
    {
        var value = await database.StringGetAsync(TabKey(id));
        return value.HasValue
            ? JsonSerializer.Deserialize<Tab>((string)value!, RedisJson.Options)
            : null;
    }

    public async Task<Tab?> GetByInviteToken(string inviteToken)
    {
        var tabId = await database.StringGetAsync(InviteKey(inviteToken));
        return tabId.HasValue ? await GetById(tabId!) : null;
    }

    public Task Create(Tab tab) => SaveTab(tab);

    public Task Update(Tab tab) => SaveTab(tab);

    public async Task Delete(string id)
    {
        var tab = await GetById(id);
        if (tab is null)
        {
            return;
        }

        var batch = database.CreateBatch();
        var tasks = new List<Task>
        {
            batch.KeyDeleteAsync(TabKey(id)),
            batch.KeyDeleteAsync(MembersKey(id)),
            batch.KeyDeleteAsync(ItemsKey(id)),
            batch.KeyDeleteAsync(InviteKey(tab.InviteToken)),
            batch.SetRemoveAsync(AllTabsKey(), id)
        };

        foreach (var userId in IndexedMemberIds(tab))
        {
            tasks.Add(batch.SetRemoveAsync(UserTabsKey(userId), id));
        }

        var itemIds = await database.SetMembersAsync(ItemsKey(id));
        foreach (var itemId in itemIds)
        {
            tasks.Add(batch.KeyDeleteAsync(ItemKey(id, itemId!)));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<TabItem>> GetItems(string tabId)
    {
        var itemIds = await database.SetMembersAsync(ItemsKey(tabId));
        if (itemIds.Length == 0)
        {
            return [];
        }

        var keys = itemIds.Select(itemId => (RedisKey)ItemKey(tabId, itemId!)).ToArray();
        var values = await database.StringGetAsync(keys);

        return values
            .Where(value => value.HasValue)
            .Select(value => JsonSerializer.Deserialize<TabItem>((string)value!, RedisJson.Options))
            .OfType<TabItem>()
            .ToArray();
    }

    public async Task AddItem(TabItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.TabId);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);

        var json = JsonSerializer.Serialize(item, RedisJson.Options);
        var batch = database.CreateBatch();
        var setItem = batch.StringSetAsync(ItemKey(item.TabId, item.Id), json);
        var indexItem = batch.SetAddAsync(ItemsKey(item.TabId), item.Id);
        batch.Execute();
        await Task.WhenAll(setItem, indexItem);
    }

    public async Task RemoveItem(string tabId, string itemId)
    {
        var batch = database.CreateBatch();
        var deleteItem = batch.KeyDeleteAsync(ItemKey(tabId, itemId));
        var removeIndex = batch.SetRemoveAsync(ItemsKey(tabId), itemId);
        batch.Execute();
        await Task.WhenAll(deleteItem, removeIndex);
    }

    public async Task<IReadOnlyList<Tab>> GetTabsForUser(string userId)
    {
        var tabIds = await database.SetMembersAsync(UserTabsKey(userId));
        if (tabIds.Length == 0)
        {
            return [];
        }

        var keys = tabIds.Select(tabId => (RedisKey)TabKey(tabId!)).ToArray();
        var values = await database.StringGetAsync(keys);

        return values
            .Where(value => value.HasValue)
            .Select(value => JsonSerializer.Deserialize<Tab>((string)value!, RedisJson.Options))
            .OfType<Tab>()
            .ToArray();
    }

    public async Task<IReadOnlyList<OwnerTabCount>> GetOwnerTabCounts()
    {
        var tabIds = await database.SetMembersAsync(AllTabsKey());
        if (tabIds.Length == 0)
        {
            return [];
        }

        var keys = tabIds.Select(tabId => (RedisKey)TabKey(tabId!)).ToArray();
        var values = await database.StringGetAsync(keys);

        return values
            .Where(value => value.HasValue)
            .Select(value => JsonSerializer.Deserialize<Tab>((string)value!, RedisJson.Options))
            .OfType<Tab>()
            .GroupBy(tab => tab.OwnerId, StringComparer.Ordinal)
            .Select(group => new OwnerTabCount(group.Key, group.Count()))
            .ToArray();
    }

    public async Task AddMember(string tabId, string userId)
    {
        var tab = await GetById(tabId) ?? throw new InvalidOperationException($"Tab '{tabId}' was not found.");
        var memberIds = tab.MemberIds.Contains(userId, StringComparer.Ordinal)
            ? tab.MemberIds
            : [.. tab.MemberIds, userId];

        await SaveTab(tab with { MemberIds = memberIds });
    }

    private async Task SaveTab(Tab tab)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tab.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(tab.OwnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tab.InviteToken);

        var existing = await GetById(tab.Id);
        var json = JsonSerializer.Serialize(tab, RedisJson.Options);
        var batch = database.CreateBatch();
        var tasks = new List<Task>
        {
            batch.StringSetAsync(TabKey(tab.Id), json),
            batch.StringSetAsync(InviteKey(tab.InviteToken), tab.Id),
            batch.SetAddAsync(AllTabsKey(), tab.Id)
        };

        if (existing is not null && !string.Equals(existing.InviteToken, tab.InviteToken, StringComparison.Ordinal))
        {
            tasks.Add(batch.KeyDeleteAsync(InviteKey(existing.InviteToken)));
        }

        foreach (var userId in IndexedMemberIds(tab))
        {
            tasks.Add(batch.SetAddAsync(MembersKey(tab.Id), userId));
            tasks.Add(batch.SetAddAsync(UserTabsKey(userId), tab.Id));
        }

        if (existing is not null)
        {
            foreach (var removedUserId in IndexedMemberIds(existing).Except(IndexedMemberIds(tab), StringComparer.Ordinal))
            {
                tasks.Add(batch.SetRemoveAsync(MembersKey(tab.Id), removedUserId));
                tasks.Add(batch.SetRemoveAsync(UserTabsKey(removedUserId), tab.Id));
            }
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }

    private static IEnumerable<string> IndexedMemberIds(Tab tab) => tab.MemberIds
        .Append(tab.OwnerId)
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.Ordinal);

    private static string TabKey(string id) => $"tab:{id}";

    private static string InviteKey(string token) => $"invite:{token}";

    private static string MembersKey(string tabId) => $"tab:{tabId}:members";

    private static string ItemsKey(string tabId) => $"tab:{tabId}:items";

    private static string ItemKey(string tabId, string itemId) => $"tab:{tabId}:item:{itemId}";

    private static string UserTabsKey(string userId) => $"user:{userId}:tabs";

    private static string AllTabsKey() => "tabs:all";
}
