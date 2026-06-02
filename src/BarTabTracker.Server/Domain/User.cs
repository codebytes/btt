namespace BarTabTracker.Server.Domain;

public sealed record User
{
    public required string Id { get; init; }
    public required string OAuthSubject { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
