using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarTabTracker.Server.Storage.Redis;

internal static class RedisJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
