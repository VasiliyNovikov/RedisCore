namespace RedisCore.Internal.Commands;

internal sealed class RightPopCommand<T>(string key) : GetValueByKeyCommand<T>(CommandNames.RPop, key);