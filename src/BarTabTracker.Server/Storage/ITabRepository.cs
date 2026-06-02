using BarTabTracker.Server.Domain;

namespace BarTabTracker.Server.Storage;

public interface ITabRepository
{
    Task<Tab?> GetById(string id);

    Task<Tab?> GetByInviteToken(string inviteToken);

    Task Create(Tab tab);

    Task Update(Tab tab);

    Task Delete(string id);

    Task<IReadOnlyList<TabItem>> GetItems(string tabId);

    Task AddItem(TabItem item);

    Task RemoveItem(string tabId, string itemId);

    Task<IReadOnlyList<Tab>> GetTabsForUser(string userId);

    Task AddMember(string tabId, string userId);
}
