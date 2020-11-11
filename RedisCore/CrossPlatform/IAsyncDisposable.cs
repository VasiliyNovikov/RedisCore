#if !NETSTANDARD2_1
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
#endif