using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class WatchCommand(string key) : VoidCommand(CommandNames.Watch, key.ToValue());