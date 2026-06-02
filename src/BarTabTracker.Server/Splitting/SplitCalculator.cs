using BarTabTracker.Server.Domain;

namespace BarTabTracker.Server.Splitting;

public sealed class SplitCalculator
{
    public SplitResult Calculate(Tab tab, IReadOnlyCollection<TabItem> items)
    {
        var memberIds = tab.MemberIds
            .Append(tab.OwnerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (memberIds.Length == 0)
        {
            throw new InvalidOperationException("A tab must have at least one member to calculate splits.");
        }

        var total = RoundMoney(items.Sum(ItemTotal));
        var equal = AllocateEqually(total, memberIds);
        var itemized = memberIds.ToDictionary(id => id, _ => 0m, StringComparer.Ordinal);

        foreach (var item in items)
        {
            var itemTotal = ItemTotal(item);
            if (item.OrderedByUserId is null)
            {
                foreach (var allocation in AllocateEqually(itemTotal, memberIds))
                {
                    itemized[allocation.Key] += allocation.Value;
                }

                continue;
            }

            if (!itemized.ContainsKey(item.OrderedByUserId))
            {
                throw new InvalidOperationException($"OrderedByUserId '{item.OrderedByUserId}' is not a tab member.");
            }

            itemized[item.OrderedByUserId] += itemTotal;
        }

        return new SplitResult(total, equal, itemized);
    }

    public static decimal ItemTotal(TabItem item) => RoundMoney(item.Price * item.Quantity);

    private static IReadOnlyDictionary<string, decimal> AllocateEqually(decimal amount, IReadOnlyCollection<string> memberIds)
    {
        if (memberIds.Count == 0)
        {
            throw new InvalidOperationException("Cannot allocate a split without members.");
        }

        var sortedMemberIds = memberIds.Order(StringComparer.Ordinal).ToArray();
        var cents = decimal.ToInt64(RoundMoney(amount) * 100m);
        var baseCents = cents / sortedMemberIds.Length;
        var remainderCents = cents % sortedMemberIds.Length;
        var allocations = new Dictionary<string, decimal>(StringComparer.Ordinal);

        for (var index = 0; index < sortedMemberIds.Length; index++)
        {
            var memberCents = baseCents + (index < remainderCents ? 1 : 0);
            allocations[sortedMemberIds[index]] = memberCents / 100m;
        }

        return allocations;
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record SplitResult(
    decimal Total,
    IReadOnlyDictionary<string, decimal> Equal,
    IReadOnlyDictionary<string, decimal> Itemized);
