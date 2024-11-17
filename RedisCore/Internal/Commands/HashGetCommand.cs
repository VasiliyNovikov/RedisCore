using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashGetCommand<T>(string key, string field) : OptionalValueCommand<T>(CommandNames.HGet, key.ToValue(), field.ToValue());