using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class VoidCommand(RedisString name, params RedisObject[] args) : Command<bool>(name, args)
{
    public override bool GetResult(RedisObject resultObject) => true;
}