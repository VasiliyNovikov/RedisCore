using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal static class CommandSwitches
{
    public static readonly RedisString PX = new RedisByteString("PX");
    public static readonly RedisString NX = new RedisByteString("NX");
    public static readonly RedisString XX = new RedisByteString("XX");
}