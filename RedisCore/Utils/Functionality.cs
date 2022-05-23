using System;

namespace RedisCore.Utils;

public class Functionality<TFunctionality, T> where TFunctionality : Functionality<TFunctionality, T>
{
    private static TFunctionality? _instance;
    public static TFunctionality Instance
    {
        get => _instance ?? throw new NotImplementedException($"There is no implementation supplied for functionality {typeof(TFunctionality)}");
        set => _instance = value;
    }
}