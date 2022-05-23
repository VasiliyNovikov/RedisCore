using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class VoidCommand : Command<bool>
{
    protected VoidCommand(RedisString name, params RedisObject[] args) 
        : base(name, args)
    {
    }

    public override bool GetResult(RedisObject resultObject) => true;
}