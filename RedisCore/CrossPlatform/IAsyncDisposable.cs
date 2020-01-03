﻿#if !NETSTANDARD21
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
#endif