using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal static class CommandNames
    {
        public static readonly RedisString Ping = new RedisByteString("PING");
        public static readonly RedisString Auth = new RedisByteString("AUTH");
        
        public static readonly RedisString Get = new RedisByteString("GET");
        public static readonly RedisString Set = new RedisByteString("SET");
        public static readonly RedisString Del = new RedisByteString("DEL");
        public static readonly RedisString PExpire = new RedisByteString("PEXPIRE");
        
        public static readonly RedisString Multi = new RedisByteString("MULTI");
        public static readonly RedisString Exec = new RedisByteString("EXEC");
        public static readonly RedisString Discard = new RedisByteString("DISCARD");
        public static readonly RedisString Watch = new RedisByteString("WATCH");
        
        public static readonly RedisString LPush = new RedisByteString("LPUSH");
        public static readonly RedisString RPush = new RedisByteString("RPUSH");
        public static readonly RedisString LPop = new RedisByteString("LPOP");
        public static readonly RedisString RPop = new RedisByteString("RPOP");
        public static readonly RedisString RPopLPush = new RedisByteString("RPOPLPUSH");
        public static readonly RedisString BRPopLPush = new RedisByteString("BRPOPLPUSH");
        public static readonly RedisString LIndex = new RedisByteString("LINDEX");
        public static readonly RedisString LLen = new RedisByteString("LLEN");

        public static readonly RedisString HGet = new RedisByteString("HGET");
        public static readonly RedisString HSet = new RedisByteString("HSET");
        public static readonly RedisString HSetNX = new RedisByteString("HSETNX");
        public static readonly RedisString HDel = new RedisByteString("HDEL");
        public static readonly RedisString HExists = new RedisByteString("HEXISTS");
        public static readonly RedisString HLen = new RedisByteString("HLEN");
        public static readonly RedisString HKeys = new RedisByteString("HKEYS");
        public static readonly RedisString HVals = new RedisByteString("HVALS");
        public static readonly RedisString HGetAll = new RedisByteString("HGETALL");
        
        public static readonly RedisString Publish = new RedisByteString("PUBLISH");
        public static readonly RedisString Subscribe = new RedisByteString("SUBSCRIBE");
        public static readonly RedisString Unsubscribe = new RedisByteString("UNSUBSCRIBE");
        
        public static readonly RedisString Eval = new RedisByteString("EVAL");
    }
}