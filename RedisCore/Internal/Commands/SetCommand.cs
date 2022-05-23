using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class SetCommand<T> : Command<bool>
{
    private static RedisObject[] PrepareParams(string key, T value, TimeSpan? expiration = null, OptimisticConcurrency concurrency = OptimisticConcurrency.None)
    {
        var paramCount = 2;
        if (expiration != null)
            paramCount += 2;
        if (concurrency != OptimisticConcurrency.None)
            ++paramCount;

        var @params = new RedisObject[paramCount];
        @params[0] = key.ToValue();
        @params[1] = value.ToValue();
        var paramIndex = 2;
        if (expiration != null)
        {
            @params[paramIndex++] = CommandSwitches.PX;
            @params[paramIndex++] = ((int)expiration.Value.TotalMilliseconds).ToValue();
        }
#pragma warning disable IDE0010 // Add missing cases to switch statement
        switch (concurrency)
        {
            case OptimisticConcurrency.IfNotExists:
                @params[paramIndex] = CommandSwitches.NX;
                break;
            case OptimisticConcurrency.IfExists:
                @params[paramIndex] = CommandSwitches.XX;
                break;
        }
#pragma warning restore IDE0010
        return @params;
    }

    public SetCommand(string key, T value, TimeSpan? expiration = null, OptimisticConcurrency concurrency = default)
        : base(CommandNames.Set, PrepareParams(key, value, expiration, concurrency))
    {
    }

    public override bool GetResult(RedisObject resultObject) => resultObject != RedisNull.Value;
}