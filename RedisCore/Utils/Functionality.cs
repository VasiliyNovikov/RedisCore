using System;
using System.Diagnostics.CodeAnalysis;

namespace RedisCore.Utils;

[SuppressMessage("Microsoft.Design", "CA1000: Do not declare static members on generic types", Justification = "Done on purpose")]
public class Functionality<TFunctionality, T> where TFunctionality : Functionality<TFunctionality, T>
{
    [SuppressMessage("Microsoft. Style", "IDE0032: Use auto property", Justification = "Can't use field feature in this particular case")]
    private static TFunctionality? _instance;
    public static TFunctionality Instance
    {
        get => _instance ?? throw new NotImplementedException($"There is no implementation supplied for functionality {typeof(TFunctionality)}");
        set => _instance = value;
    }
}