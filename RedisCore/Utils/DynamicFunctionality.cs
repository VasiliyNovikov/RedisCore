using System;

namespace RedisCore.Utils;

public abstract class DynamicFunctionality<TDynamicFunctionality, TFunctionality, T>
    where TFunctionality : Functionality<TFunctionality, T>
    where TDynamicFunctionality : DynamicFunctionality<TDynamicFunctionality, TFunctionality, T>, new()
{
    private static readonly TDynamicFunctionality DynInstance = new();

    protected abstract TFunctionality CreateInstance();

    protected static TFunctionality Instance
    {
        get
        {
            try
            {
                return Functionality<TFunctionality, T>.Instance;
            }
            catch (NotImplementedException)
            {
                return Functionality<TFunctionality, T>.Instance = DynInstance.CreateInstance();
            }
        }
    }
}