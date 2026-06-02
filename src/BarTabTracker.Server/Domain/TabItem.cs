namespace BarTabTracker.Server.Domain;

public sealed record TabItem
{
    public required string Id { get; init; }
    public required string TabId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required int Quantity { get; init; }
    public string? OrderedByUserId { get; init; }
}
