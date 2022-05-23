﻿using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class HashGetCommand<T> : OptionalValueCommand<T>
{
    public HashGetCommand(string key, string field) : 
        base(CommandNames.HGet, key.ToValue(), field.ToValue())
    {
    }
}