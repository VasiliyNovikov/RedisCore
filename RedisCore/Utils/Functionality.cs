using System;

namespace RedisCore.Utils
{
    public class Functionality<T>
    {
        public static class Implementation<TFunctionality> where TFunctionality : Functionality<T>
        {
            private static TFunctionality _instance;
            public static TFunctionality Instance
            {
                get => _instance ?? throw new NotImplementedException($"There is no implementation supplied for functionality {typeof(TFunctionality)}");
                set => _instance = value;
            }
        }
    }
}