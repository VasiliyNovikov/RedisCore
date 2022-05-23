namespace RedisCore.Internal.Protocol;

internal sealed class RedisNull : RedisValueObject
{
    private RedisNull()
    {
    }
            
    public static readonly RedisNull Value = new();
}