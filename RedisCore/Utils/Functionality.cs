using System;
using System.Diagnostics.CodeAnalysis;

namespace RedisCore.Utils;

[SuppressMessage("Microsoft.Design", "CA1000: Do not declare static members on generic types", Justification = "It is intentional")]
public abstract class Functionality<TFunctionality, T> where TFunctionality : Functionality<TFunctionality, T>
{
    private static TFunctionality? _instance;
    public static TFunctionality Instance
    {
        [SuppressMessage("Microsoft.Design", "CA1065: Do not raise exceptions in unexpected locations", Justification = "By design")]
        get => _instance ?? throw new NotImplementedException($"There is no implementation supplied for functionality {typeof(TFunctionality)}");
        set => _instance = value;
    }
}