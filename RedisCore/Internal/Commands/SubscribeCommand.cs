using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class SubscribeCommand(string channel) : VoidCommand(CommandNames.Subscribe, channel.ToValue());