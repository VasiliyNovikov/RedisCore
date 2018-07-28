using System;

namespace RedisCore.Internal.Protocol
{
    public static class FormattedSize
    {
        public static int Value<T>() where T : struct
        {
            return Container<T>.Value;
        }

        static FormattedSize()
        {
            Container<Int32>.Value = ProtocolHandler.Encoding.GetByteCount(Int32.MinValue.ToString());
            Container<Int64>.Value = ProtocolHandler.Encoding.GetByteCount(Int64.MinValue.ToString());
            Container<Double>.Value = 32;
            Container<Guid>.Value = ProtocolHandler.Encoding.GetByteCount(Guid.Empty.ToString()) + 2;
        }
        
        private static class Container<T> where T : struct
        {
            // ReSharper disable once StaticMemberInGenericType
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static int Value = -1;
        }
    }
}