using BarTabTracker.Server.Domain;

namespace BarTabTracker.Server.Storage;

public interface IUserRepository
{
    Task<User?> GetById(string id);

    Task<User?> GetByOAuthSubject(string oauthSubject);

    Task Upsert(User user);
}
