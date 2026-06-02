using BarTabTracker.Server.Domain;

namespace BarTabTracker.Server.Api;

public sealed record UserResponse(string Id, string DisplayName, string? AvatarUrl, DateTimeOffset CreatedAt);

public sealed record BarLocationDto(string Name, double Lat, double Lng);

public sealed record CreateTabRequest(string? Name, BarLocationDto? Bar, string? Currency);

public sealed record AddItemRequest(string? Name, decimal Price, int Quantity, string? OrderedByUserId);

public sealed record DevLoginRequest(string? DisplayName, string? AvatarUrl);

public sealed record TabSummaryResponse(
    string Id,
    string Name,
    string OwnerId,
    TabStatus Status,
    string Currency,
    BarLocationDto? Bar,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    string InviteToken,
    IReadOnlyList<string> MemberIds);

public sealed record TabItemResponse(
    string Id,
    string TabId,
    string Name,
    decimal Price,
    int Quantity,
    string? OrderedByUserId,
    decimal Total);

public sealed record SplitResponse(
    decimal Total,
    IReadOnlyDictionary<string, decimal> Equal,
    IReadOnlyDictionary<string, decimal> Itemized);

public sealed record TabDetailResponse(
    string Id,
    string Name,
    string OwnerId,
    TabStatus Status,
    string Currency,
    BarLocationDto? Bar,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    string InviteToken,
    IReadOnlyList<string> MemberIds,
    IReadOnlyList<TabItemResponse> Items,
    SplitResponse Split);

public sealed record ReverseGeocodeResponse(string? BarName, string? Country, string Currency, bool IsFallback);

public sealed record LeaderboardResponse(string UserId, string DisplayName, string? AvatarUrl, int TabCount);
