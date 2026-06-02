using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BarTabTracker.Server.Domain;
using BarTabTracker.Server.Storage;
using StackExchange.Redis;

namespace BarTabTracker.Server.Storage.Redis;

public sealed class RedisUserRepository(IConnectionMultiplexer connection) : IUserRepository
{
    private readonly IDatabase database = connection.GetDatabase();

    public async Task<User?> GetById(string id)
    {
        var value = await database.StringGetAsync(UserKey(id));
        return value.HasValue
            ? JsonSerializer.Deserialize<User>((string)value!, RedisJson.Options)
            : null;
    }

    public async Task<User?> GetByOAuthSubject(string oauthSubject)
    {
        var userId = await database.StringGetAsync(OAuthSubjectKey(oauthSubject));
        return userId.HasValue ? await GetById(userId!) : null;
    }

    public async Task Upsert(User user)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(user.OAuthSubject);

        var json = JsonSerializer.Serialize(user, RedisJson.Options);
        await database.StringSetAsync(UserKey(user.Id), json);
        await database.StringSetAsync(OAuthSubjectKey(user.OAuthSubject), user.Id);
    }

    private static string UserKey(string id) => $"user:{id}";

    private static string OAuthSubjectKey(string oauthSubject)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(oauthSubject));
        return $"user:oauth:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
