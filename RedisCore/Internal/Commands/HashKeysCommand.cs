using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashKeysCommand(string key) : Command<HashSet<string>>(CommandNames.HKeys, key.ToValue());