#if !NET6_0_OR_GREATER
using System.Threading.Tasks;

namespace System;

public interface IAsyncDisposable
{
    ValueTask DisposeAsync();
}
#endif