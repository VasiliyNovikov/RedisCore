using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashKeysCommand : Command<HashSet<string>>
{
    public HashKeysCommand(string key) 
        : base(CommandNames.HKeys, key.ToValue())
    {
    }
}