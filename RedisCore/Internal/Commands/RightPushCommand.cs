namespace RedisCore.Internal.Commands;

internal sealed class RightPushCommand<T>(string key, T value) : PushCommand<T>(CommandNames.RPush, key, value);