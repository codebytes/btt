using System.Security.Claims;
using System.Security.Cryptography;
using BarTabTracker.Server.Auth;
using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Leaderboard;
using BarTabTracker.Server.Location;
using BarTabTracker.Server.Splitting;
using BarTabTracker.Server.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace BarTabTracker.Server.Api;

public static class ApiEndpointExtensions
{
    public static RouteGroupBuilder MapBarTabApi(this WebApplication app, bool microsoftAuthEnabled)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/me", async (HttpContext httpContext, ICurrentUserService currentUserService) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            return user is null ? Results.Unauthorized() : Results.Ok(ToUserResponse(user));
        }).RequireAuthorization();

        api.MapGet("/tabs", async (HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var userTabs = await tabs.GetTabsForUser(user.Id);
            return Results.Ok(userTabs.Select(ToTabSummary).OrderByDescending(tab => tab.CreatedAt));
        }).RequireAuthorization();

        api.MapPost("/tabs", async (CreateTabRequest request, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var validation = ValidateCreateTab(request);
            if (validation.Count > 0)
            {
                return Results.ValidationProblem(validation);
            }

            var now = DateTimeOffset.UtcNow;
            var tab = new Tab
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = request.Name!.Trim(),
                OwnerId = user.Id,
                Status = TabStatus.Open,
                Currency = NormalizeCurrency(request.Currency),
                Bar = request.Bar is null ? null : new BarLocation
                {
                    Name = request.Bar.Name.Trim(),
                    Lat = request.Bar.Lat,
                    Lng = request.Bar.Lng
                },
                CreatedAt = now,
                ClosedAt = null,
                InviteToken = GenerateInviteToken(),
                MemberIds = [user.Id]
            };

            await tabs.Create(tab);
            return Results.Created($"/api/tabs/{tab.Id}", ToTabSummary(tab));
        }).RequireAuthorization();

        api.MapGet("/tabs/{id}", async (string id, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs, SplitCalculator splitCalculator) =>
        {
            var access = await GetMemberTab(id, httpContext, currentUserService, tabs);
            if (access.Result is not null)
            {
                return access.Result;
            }

            return await BuildTabDetail(access.Tab!, tabs, splitCalculator);
        }).RequireAuthorization();

        api.MapPost("/tabs/{id}/close", async (string id, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs, SplitCalculator splitCalculator) =>
        {
            var access = await GetMemberTab(id, httpContext, currentUserService, tabs);
            if (access.Result is not null)
            {
                return access.Result;
            }

            var user = access.User!;
            var tab = access.Tab!;
            if (!string.Equals(tab.OwnerId, user.Id, StringComparison.Ordinal))
            {
                return Problem(StatusCodes.Status403Forbidden, "Only the tab owner can close this tab.");
            }

            if (tab.Status == TabStatus.Closed)
            {
                return await BuildTabDetail(tab, tabs, splitCalculator);
            }

            var closedTab = tab with { Status = TabStatus.Closed, ClosedAt = DateTimeOffset.UtcNow };
            await tabs.Update(closedTab);
            return await BuildTabDetail(closedTab, tabs, splitCalculator);
        }).RequireAuthorization();

        api.MapPost("/tabs/{id}/items", async (string id, AddItemRequest request, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs, SplitCalculator splitCalculator) =>
        {
            var access = await GetMemberTab(id, httpContext, currentUserService, tabs);
            if (access.Result is not null)
            {
                return access.Result;
            }

            var tab = access.Tab!;
            if (tab.Status == TabStatus.Closed)
            {
                return Problem(StatusCodes.Status409Conflict, "Closed tabs cannot be modified.");
            }

            var validation = ValidateAddItem(request, tab);
            if (validation.Count > 0)
            {
                return Results.ValidationProblem(validation);
            }

            var item = new TabItem
            {
                Id = Guid.NewGuid().ToString("n"),
                TabId = tab.Id,
                Name = request.Name!.Trim(),
                Price = request.Price,
                Quantity = request.Quantity,
                OrderedByUserId = string.IsNullOrWhiteSpace(request.OrderedByUserId) ? null : request.OrderedByUserId
            };

            await tabs.AddItem(item);
            return await BuildTabDetail(tab, tabs, splitCalculator);
        }).RequireAuthorization();

        api.MapDelete("/tabs/{id}/items/{itemId}", async (string id, string itemId, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs) =>
        {
            var access = await GetMemberTab(id, httpContext, currentUserService, tabs);
            if (access.Result is not null)
            {
                return access.Result;
            }

            var tab = access.Tab!;
            if (tab.Status == TabStatus.Closed)
            {
                return Problem(StatusCodes.Status409Conflict, "Closed tabs cannot be modified.");
            }

            var items = await tabs.GetItems(id);
            if (!items.Any(item => string.Equals(item.Id, itemId, StringComparison.Ordinal)))
            {
                return Problem(StatusCodes.Status404NotFound, "Item was not found.");
            }

            await tabs.RemoveItem(id, itemId);
            return Results.NoContent();
        }).RequireAuthorization();

        api.MapPost("/tabs/join/{token}", async (string token, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs, SplitCalculator splitCalculator) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var tab = await tabs.GetByInviteToken(token);
            if (tab is null)
            {
                return Problem(StatusCodes.Status404NotFound, "Invite token was not found.");
            }

            if (tab.Status == TabStatus.Closed)
            {
                return Problem(StatusCodes.Status409Conflict, "Closed tabs cannot be joined.");
            }

            await tabs.AddMember(tab.Id, user.Id);
            var updatedTab = await tabs.GetById(tab.Id) ?? tab;
            return await BuildTabDetail(updatedTab, tabs, splitCalculator);
        }).RequireAuthorization();

        api.MapGet("/history", async (HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var userTabs = await tabs.GetTabsForUser(user.Id);
            return Results.Ok(userTabs
                .Where(tab => tab.Status == TabStatus.Closed)
                .Select(ToTabSummary)
                .OrderByDescending(tab => tab.ClosedAt));
        }).RequireAuthorization();

        api.MapGet("/leaderboard", async (int? limit, HttpContext httpContext, ICurrentUserService currentUserService, LeaderboardService leaderboard) =>
        {
            var user = await currentUserService.GetCurrentUser(httpContext);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var entries = await leaderboard.GetLeaderboard(limit);
            return Results.Ok(entries.Select(ToLeaderboardResponse));
        }).RequireAuthorization();

        api.MapGet("/geocode/reverse", async (double lat, double lng, IReverseGeocoder geocoder, CancellationToken cancellationToken) =>
        {
            if (lat is < -90 or > 90 || lng is < -180 or > 180)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(lat)] = ["Latitude must be between -90 and 90."],
                    [nameof(lng)] = ["Longitude must be between -180 and 180."]
                });
            }

            return Results.Ok(await geocoder.ReverseGeocode(lat, lng, cancellationToken));
        }).RequireAuthorization().RequireRateLimiting("geocode");

        api.MapGet("/auth/login", (HttpContext httpContext) => microsoftAuthEnabled
            ? Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme])
            : Problem(StatusCodes.Status503ServiceUnavailable, "Microsoft Entra ID credentials are not configured. Use /api/auth/dev-login in Development."));

        api.MapPost("/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        if (app.Environment.IsDevelopment())
        {
            api.MapPost("/auth/dev-login", async (DevLoginRequest? request, HttpContext httpContext, IUserRepository users) =>
            {
                const string subject = "dev:local";
                var existing = await users.GetByOAuthSubject(subject);
                var user = new User
                {
                    Id = existing?.Id ?? "dev-user",
                    OAuthSubject = subject,
                    DisplayName = string.IsNullOrWhiteSpace(request?.DisplayName) ? "Development User" : request.DisplayName.Trim(),
                    AvatarUrl = request?.AvatarUrl,
                    CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow
                };

                await users.Upsert(user);
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.OAuthSubject),
                    new(BarTabClaimTypes.OAuthSubject, user.OAuthSubject),
                    new(ClaimTypes.Name, user.DisplayName)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                return Results.Ok(ToUserResponse(user));
            });
        }

        return api;
    }

    private static async Task<IResult> BuildTabDetail(Tab tab, ITabRepository tabs, SplitCalculator splitCalculator)
    {
        var items = await tabs.GetItems(tab.Id);
        try
        {
            var split = splitCalculator.Calculate(tab, items);
            return Results.Ok(ToTabDetail(tab, items, split));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static async Task<TabAccess> GetMemberTab(string id, HttpContext httpContext, ICurrentUserService currentUserService, ITabRepository tabs)
    {
        var user = await currentUserService.GetCurrentUser(httpContext);
        if (user is null)
        {
            return new TabAccess(null, null, Results.Unauthorized());
        }

        var tab = await tabs.GetById(id);
        if (tab is null || !IsMember(tab, user.Id))
        {
            return new TabAccess(user, tab, Problem(StatusCodes.Status404NotFound, "Tab was not found."));
        }

        return new TabAccess(user, tab, null);
    }

    private static Dictionary<string, string[]> ValidateCreateTab(CreateTabRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Tab name is required."];
        }

        if (request.Bar is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Bar.Name))
            {
                errors[nameof(request.Bar.Name)] = ["Bar name is required when bar location is supplied."];
            }

            if (request.Bar.Lat is < -90 or > 90)
            {
                errors[nameof(request.Bar.Lat)] = ["Latitude must be between -90 and 90."];
            }

            if (request.Bar.Lng is < -180 or > 180)
            {
                errors[nameof(request.Bar.Lng)] = ["Longitude must be between -180 and 180."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Currency) && request.Currency.Trim().Length != 3)
        {
            errors[nameof(request.Currency)] = ["Currency must be a 3-letter ISO code."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateAddItem(AddItemRequest request, Tab tab)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Item name is required."];
        }

        if (request.Price < 0m)
        {
            errors[nameof(request.Price)] = ["Price cannot be negative."];
        }

        if (request.Quantity <= 0)
        {
            errors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        }

        if (!string.IsNullOrWhiteSpace(request.OrderedByUserId) && !IsMember(tab, request.OrderedByUserId))
        {
            errors[nameof(request.OrderedByUserId)] = ["OrderedByUserId must be a tab member, or null for a shared item."];
        }

        return errors;
    }

    private static bool IsMember(Tab tab, string userId) => string.Equals(tab.OwnerId, userId, StringComparison.Ordinal)
        || tab.MemberIds.Contains(userId, StringComparer.Ordinal);

    private static UserResponse ToUserResponse(User user) => new(user.Id, user.DisplayName, user.AvatarUrl, user.CreatedAt);

    private static LeaderboardResponse ToLeaderboardResponse(LeaderboardEntry entry) =>
        new(entry.UserId, entry.DisplayName, entry.AvatarUrl, entry.TabCount);

    private static TabSummaryResponse ToTabSummary(Tab tab) => new(
        tab.Id,
        tab.Name,
        tab.OwnerId,
        tab.Status,
        tab.Currency,
        tab.Bar is null ? null : new BarLocationDto(tab.Bar.Name, tab.Bar.Lat, tab.Bar.Lng),
        tab.CreatedAt,
        tab.ClosedAt,
        tab.InviteToken,
        tab.MemberIds);

    private static TabDetailResponse ToTabDetail(Tab tab, IReadOnlyCollection<TabItem> items, SplitResult split) => new(
        tab.Id,
        tab.Name,
        tab.OwnerId,
        tab.Status,
        tab.Currency,
        tab.Bar is null ? null : new BarLocationDto(tab.Bar.Name, tab.Bar.Lat, tab.Bar.Lng),
        tab.CreatedAt,
        tab.ClosedAt,
        tab.InviteToken,
        tab.MemberIds,
        items.Select(ToTabItem).ToArray(),
        new SplitResponse(split.Total, split.Equal, split.Itemized));

    private static TabItemResponse ToTabItem(TabItem item) => new(
        item.Id,
        item.TabId,
        item.Name,
        item.Price,
        item.Quantity,
        item.OrderedByUserId,
        SplitCalculator.ItemTotal(item));

    private static string NormalizeCurrency(string? currency) => string.IsNullOrWhiteSpace(currency)
        ? "USD"
        : currency.Trim().ToUpperInvariant();

    private static string GenerateInviteToken()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static IResult Problem(int statusCode, string detail) => Results.Problem(statusCode: statusCode, detail: detail);

    private sealed record TabAccess(User? User, Tab? Tab, IResult? Result);
}
