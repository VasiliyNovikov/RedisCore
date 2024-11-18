namespace RedisCore.Internal.Commands;

internal sealed class GetCommand<T>(string key) : GetValueByKeyCommand<T>(CommandNames.Get, key);