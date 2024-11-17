namespace RedisCore.Internal.Commands;

internal sealed class LeftPopCommand<T>(string key) : GetValueByKeyCommand<T>(CommandNames.LPop, key);