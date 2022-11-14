#if !NET6_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

public sealed class MaybeNullWhenAttribute : Attribute
{
    public bool ReturnValue { get; }
    public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
}
#endif