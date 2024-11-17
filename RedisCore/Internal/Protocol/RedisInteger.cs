using System.Globalization;

namespace RedisCore.Internal.Protocol;

internal sealed class RedisInteger(long value) : RedisValueObject
{
    public long Value => value;

    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
}