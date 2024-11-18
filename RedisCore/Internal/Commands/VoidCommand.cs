using RedisCore.Internal.Protocol;
using System;

namespace RedisCore.Internal.Commands;

internal abstract class VoidCommand(RedisString name, params ReadOnlySpan<RedisObject> args) : Command<bool>(name, args)
{
    public override bool GetResult(RedisObject resultObject) => true;
}