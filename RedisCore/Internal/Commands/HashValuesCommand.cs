using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class HashValuesCommand<T> : Command<T[]>
{
    public HashValuesCommand(string key)
        : base(CommandNames.HVals, key.ToValue())
    {
    }
}