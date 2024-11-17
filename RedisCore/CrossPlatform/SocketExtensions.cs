#if NETSTANDARD2_0
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace System.Net.Sockets;

public static class SocketExtensions
{
    public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            return await SocketTaskExtensions.ReceiveAsync(socket, segment, socketFlags);

        using var rentedBuffer = new RentedBuffer<byte>(buffer.Length);
        var result = await SocketTaskExtensions.ReceiveAsync(socket, rentedBuffer.Segment, socketFlags);
        rentedBuffer.Memory.CopyTo(buffer);
        return result;
    }

    public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
            return await SocketTaskExtensions.SendAsync(socket, segment, socketFlags);

        using var rentedBuffer = new RentedBuffer<byte>(buffer.Length);
        buffer.CopyTo(rentedBuffer);
        return await SocketTaskExtensions.SendAsync(socket, rentedBuffer.Segment, socketFlags);
    }
}
#endif