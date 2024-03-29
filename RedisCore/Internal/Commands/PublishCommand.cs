﻿using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class PublishCommand<T> : Command<int>
{
    public PublishCommand(string channel, T message) 
        : base(CommandNames.Publish, channel.ToValue(), message.ToValue())
    {
    }
}