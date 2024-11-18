using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashValuesCommand<T>(string key) : Command<T[]>(CommandNames.HVals, key.ToValue());