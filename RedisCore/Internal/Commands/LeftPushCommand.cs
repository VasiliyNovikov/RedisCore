namespace RedisCore.Internal.Commands;

internal sealed class LeftPushCommand<T>(string key, T value) : PushCommand<T>(CommandNames.LPush, key, value);