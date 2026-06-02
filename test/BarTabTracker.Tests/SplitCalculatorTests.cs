using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Splitting;

namespace BarTabTracker.Tests;

public sealed class SplitCalculatorTests
{
    private static readonly SplitCalculator Calculator = new();

    public static TheoryData<string, string[], ItemSpec[], decimal, Dictionary<string, decimal>> EqualSplitCases => new()
    {
        {
            "E01_evenly_divisible_total",
            ["u1", "u2"],
            [new("Total", 10.00m, 1, "u1")],
            10.00m,
            new() { ["u1"] = 5.00m, ["u2"] = 5.00m }
        },
        {
            "E02_non_divisible_total_one_remainder_cent",
            ["u1", "u2", "u3"],
            [new("Total", 10.00m, 1, "u1")],
            10.00m,
            new() { ["u1"] = 3.34m, ["u2"] = 3.33m, ["u3"] = 3.33m }
        },
        {
            "E03_non_divisible_total_two_remainder_cents",
            ["u1", "u2", "u3"],
            [new("Total", 10.01m, 1, "u1")],
            10.01m,
            new() { ["u1"] = 3.34m, ["u2"] = 3.34m, ["u3"] = 3.33m }
        },
        {
            "E04_remainder_uses_sorted_ids_not_input_order",
            ["u3", "u1", "u2"],
            [new("Total", 10.00m, 1, "u3")],
            10.00m,
            new() { ["u1"] = 3.34m, ["u2"] = 3.33m, ["u3"] = 3.33m }
        },
        {
            "E05_empty_tab_with_members",
            ["u1", "u2", "u3"],
            [],
            0.00m,
            new() { ["u1"] = 0.00m, ["u2"] = 0.00m, ["u3"] = 0.00m }
        },
        {
            "E06_single_member_pays_full_amount",
            ["u1"],
            [new("Total", 12.34m, 1, "u1")],
            12.34m,
            new() { ["u1"] = 12.34m }
        },
        {
            "E07_zero_price_total",
            ["u1", "u2"],
            [new("Zero", 0.00m, 1, "u1")],
            0.00m,
            new() { ["u1"] = 0.00m, ["u2"] = 0.00m }
        },
        {
            "E08_currency_rounding_to_two_decimals",
            ["u1", "u2"],
            [new("Fractional", 10.005m, 1, "u1")],
            10.01m,
            new() { ["u1"] = 5.01m, ["u2"] = 5.00m }
        },
        {
            "E09_exact_sum_invariant_on_large_total",
            ["u1", "u2", "u3", "u4", "u5", "u6", "u7"],
            [new("Large", 999999.99m, 1, "u1")],
            999999.99m,
            new()
            {
                ["u1"] = 142857.15m,
                ["u2"] = 142857.14m,
                ["u3"] = 142857.14m,
                ["u4"] = 142857.14m,
                ["u5"] = 142857.14m,
                ["u6"] = 142857.14m,
                ["u7"] = 142857.14m
            }
        }
    };

    public static TheoryData<string, string[], ItemSpec[], decimal, Dictionary<string, decimal>> ItemizedSplitCases => new()
    {
        {
            "I01_each_item_charged_to_ordering_member",
            ["u1", "u2"],
            [new("Beer", 6.00m, 1, "u1"), new("Wine", 8.00m, 1, "u2")],
            14.00m,
            new() { ["u1"] = 6.00m, ["u2"] = 8.00m }
        },
        {
            "I02_shared_item_splits_evenly",
            ["u1", "u2"],
            [new("Fries", 6.00m, 1, null)],
            6.00m,
            new() { ["u1"] = 3.00m, ["u2"] = 3.00m }
        },
        {
            "I03_shared_item_with_one_remainder_cent",
            ["u1", "u2", "u3"],
            [new("Nachos", 10.00m, 1, null)],
            10.00m,
            new() { ["u1"] = 3.34m, ["u2"] = 3.33m, ["u3"] = 3.33m }
        },
        {
            "I04_mixed_owned_and_shared_items",
            ["u1", "u2", "u3"],
            [new("Burger", 12.00m, 1, "u2"), new("Wings", 9.00m, 1, null), new("Soda", 3.00m, 1, "u1")],
            24.00m,
            new() { ["u1"] = 6.00m, ["u2"] = 15.00m, ["u3"] = 3.00m }
        },
        {
            "I05_member_who_ordered_nothing_still_pays_shared_items",
            ["u1", "u2", "u3"],
            [new("Pasta", 15.00m, 1, "u1"), new("Bread", 3.00m, 1, null)],
            18.00m,
            new() { ["u1"] = 16.00m, ["u2"] = 1.00m, ["u3"] = 1.00m }
        },
        {
            "I06_member_who_ordered_nothing_and_no_shared_items_pays_zero",
            ["u1", "u2", "u3"],
            [new("Cocktail", 7.50m, 1, "u1"), new("Mocktail", 5.00m, 1, "u2")],
            12.50m,
            new() { ["u1"] = 7.50m, ["u2"] = 5.00m, ["u3"] = 0.00m }
        },
        {
            "I07_zero_price_item_does_not_alter_shares",
            ["u1", "u2"],
            [new("Promo Snack", 0.00m, 1, "u1"), new("Pizza", 10.00m, 1, null)],
            10.00m,
            new() { ["u1"] = 5.00m, ["u2"] = 5.00m }
        },
        {
            "I08_large_quantity_item",
            ["u1", "u2"],
            [new("Oysters", 1.25m, 1000, "u2")],
            1250.00m,
            new() { ["u1"] = 0.00m, ["u2"] = 1250.00m }
        },
        {
            "I09_quantity_with_shared_allocation_and_remainder",
            ["u1", "u2", "u3"],
            [new("Sliders", 2.50m, 5, null)],
            12.50m,
            new() { ["u1"] = 4.17m, ["u2"] = 4.17m, ["u3"] = 4.16m }
        },
        {
            "I10_multiple_shared_items_distribute_remainder_per_item",
            ["u1", "u2", "u3"],
            [new("Chips", 1.00m, 1, null), new("Salsa", 1.00m, 1, null)],
            2.00m,
            new() { ["u1"] = 0.68m, ["u2"] = 0.66m, ["u3"] = 0.66m }
        },
        {
            "I11_fractional_cent_item_total_rounds_before_allocation",
            ["u1", "u2"],
            [new("Discounted Tapas", 3.335m, 3, null)],
            10.01m,
            new() { ["u1"] = 5.01m, ["u2"] = 5.00m }
        },
        {
            "I12_empty_item_list_with_members",
            ["u1", "u2"],
            [],
            0.00m,
            new() { ["u1"] = 0.00m, ["u2"] = 0.00m }
        },
        {
            "I13_single_member_receives_full_itemized_and_shared_totals",
            ["u1"],
            [new("Beer", 6.00m, 1, "u1"), new("Shared Pretzel", 4.50m, 1, null)],
            10.50m,
            new() { ["u1"] = 10.50m }
        },
        {
            "I14_exact_sum_invariant_on_mixed_tab",
            ["u1", "u2", "u3", "u4"],
            [new("Steak", 31.99m, 1, "u4"), new("Apps", 10.00m, 1, null), new("Dessert", 7.25m, 2, "u2")],
            56.49m,
            new() { ["u1"] = 2.50m, ["u2"] = 17.00m, ["u3"] = 2.50m, ["u4"] = 34.49m }
        }
    };

    [Theory]
    [MemberData(nameof(EqualSplitCases))]
    public void Calculate_equal_split_matches_spec(
        string caseName,
        string[] memberIds,
        ItemSpec[] itemSpecs,
        decimal expectedTotal,
        Dictionary<string, decimal> expectedShares)
    {
        var result = Calculator.Calculate(CreateTab(memberIds), CreateItems(itemSpecs));

        Assert.Equal(expectedTotal, result.Total);
        AssertShares(caseName, expectedShares, result.Equal, expectedTotal);
    }

    [Theory]
    [MemberData(nameof(ItemizedSplitCases))]
    public void Calculate_itemized_split_matches_spec(
        string caseName,
        string[] memberIds,
        ItemSpec[] itemSpecs,
        decimal expectedTotal,
        Dictionary<string, decimal> expectedShares)
    {
        var result = Calculator.Calculate(CreateTab(memberIds), CreateItems(itemSpecs));

        Assert.Equal(expectedTotal, result.Total);
        AssertShares(caseName, expectedShares, result.Itemized, expectedTotal);
    }

    private static Tab CreateTab(IReadOnlyList<string> memberIds)
    {
        if (memberIds.Count == 0)
        {
            throw new ArgumentException("Spec cases require at least one member.", nameof(memberIds));
        }

        return new Tab
        {
            Id = "tab-1",
            Name = "QA tab",
            OwnerId = memberIds[0],
            Status = TabStatus.Open,
            Currency = "USD",
            CreatedAt = DateTimeOffset.UnixEpoch,
            InviteToken = "invite-token",
            MemberIds = memberIds.Skip(1).ToArray()
        };
    }

    private static IReadOnlyCollection<TabItem> CreateItems(IReadOnlyList<ItemSpec> specs) => specs
        .Select((spec, index) => new TabItem
        {
            Id = $"item-{index + 1}",
            TabId = "tab-1",
            Name = spec.Name,
            Price = spec.Price,
            Quantity = spec.Quantity,
            OrderedByUserId = spec.OrderedByUserId
        })
        .ToArray();

    private static void AssertShares(
        string caseName,
        IReadOnlyDictionary<string, decimal> expected,
        IReadOnlyDictionary<string, decimal> actual,
        decimal expectedTotal)
    {
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));

        foreach (var (memberId, expectedShare) in expected.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            Assert.Equal(expectedShare, actual[memberId]);
            Assert.Equal(decimal.Round(actual[memberId], 2, MidpointRounding.AwayFromZero), actual[memberId]);
        }

        Assert.Equal(expectedTotal, actual.Values.Sum());
        Assert.True(actual.Values.All(share => share >= 0m), $"{caseName} should not produce negative shares.");
    }

    public sealed record ItemSpec(string Name, decimal Price, int Quantity, string? OrderedByUserId);
}
