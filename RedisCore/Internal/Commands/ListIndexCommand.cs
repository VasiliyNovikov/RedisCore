using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ListIndexCommand<T>(string key, int index) : OptionalValueCommand<T>(CommandNames.LIndex, key.ToValue(), index.ToValue());