namespace BarTabTracker.Server.Domain;

public sealed record Tab
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string OwnerId { get; init; }
    public required TabStatus Status { get; init; }
    public required string Currency { get; init; }
    public BarLocation? Bar { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public required string InviteToken { get; init; }
    public IReadOnlyList<string> MemberIds { get; init; } = [];
}

public enum TabStatus
{
    Open,
    Closed
}

public sealed record BarLocation
{
    public required string Name { get; init; }
    public required double Lat { get; init; }
    public required double Lng { get; init; }
}
