using System.Security.Claims;
using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Storage;

namespace BarTabTracker.Server.Auth;

public interface ICurrentUserService
{
    Task<User?> GetCurrentUser(HttpContext httpContext);
}

public sealed class CurrentUserService(IUserRepository users) : ICurrentUserService
{
    public Task<User?> GetCurrentUser(HttpContext httpContext)
    {
        var oauthSubject = httpContext.User.FindFirstValue(BarTabClaimTypes.OAuthSubject)
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(oauthSubject)
            ? Task.FromResult<User?>(null)
            : users.GetByOAuthSubject(oauthSubject);
    }
}
