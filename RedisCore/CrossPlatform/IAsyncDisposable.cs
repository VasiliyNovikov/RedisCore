#if !NETSTANDARD21
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncDisposable
    {
        public ValueTask DisposeAsync();
    }
}
#endif