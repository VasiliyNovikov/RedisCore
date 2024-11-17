using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class PublishCommand<T>(string channel, T message) : Command<int>(CommandNames.Publish, channel.ToValue(), message.ToValue());