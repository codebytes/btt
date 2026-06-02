using System.Security.Claims;
using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BarTabTracker.Server.Auth;

public static class AuthServiceCollectionExtensions
{
    public static bool AddBarTabAuthentication(this WebApplicationBuilder builder)
    {
        var tenantId = builder.Configuration["Authentication:Microsoft:TenantId"];
        var clientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
        var hasMicrosoftCredentials = !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret);
        var normalizedTenantId = string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId.Trim();

        var authentication = builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "BarTabTracker.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/api/auth/login";
                options.LogoutPath = "/api/auth/logout";
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

        if (hasMicrosoftCredentials)
        {
            authentication.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{normalizedTenantId}/v2.0";
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.CallbackPath = "/api/auth/callback";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                options.Events.OnTokenValidated = async context =>
                {
                    var principal = context.Principal
                        ?? throw new InvalidOperationException("Microsoft Entra ID did not return a principal.");
                    var subject = principal.FindFirstValue("oid")
                        ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirstValue("sub")
                        ?? throw new InvalidOperationException("Microsoft Entra ID did not return an oid or sub claim.");
                    var displayName = principal.FindFirstValue("name")
                        ?? principal.FindFirstValue(ClaimTypes.Name)
                        ?? principal.FindFirstValue("preferred_username")
                        ?? principal.FindFirstValue(ClaimTypes.Email)
                        ?? "Microsoft User";

                    if (principal.Identity is ClaimsIdentity identity
                        && !identity.HasClaim(claim => claim.Type == BarTabClaimTypes.OAuthSubject))
                    {
                        identity.AddClaim(new Claim(BarTabClaimTypes.OAuthSubject, subject));
                    }

                    var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    var existing = await users.GetByOAuthSubject(subject);
                    var user = new User
                    {
                        Id = existing?.Id ?? Guid.NewGuid().ToString("n"),
                        OAuthSubject = subject,
                        DisplayName = displayName,
                        AvatarUrl = existing?.AvatarUrl,
                        CreatedAt = existing?.CreatedAt ?? DateTimeOffset.UtcNow
                    };

                    await users.Upsert(user);
                };
            });
        }

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        return hasMicrosoftCredentials;
    }
}
