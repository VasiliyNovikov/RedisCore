using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashGetAllCommand<T>(string key) : Command<Dictionary<string, T>>(CommandNames.HGetAll, key.ToValue())
{
    public override Dictionary<string, T> GetResult(RedisObject resultObject)
    {
        var arrayItems = ((RedisArray)resultObject).Items;
        var result = new Dictionary<string, T>(arrayItems.Length / 2);
        for (var i = 0; i < arrayItems.Length; i += 2)
        {
            var key = ((RedisValueObject)arrayItems[i]).To<string>();
            var value = ((RedisValueObject)arrayItems[i + 1]).To<T>();
            result.Add(key, value);
        }

        return result;
    }
}