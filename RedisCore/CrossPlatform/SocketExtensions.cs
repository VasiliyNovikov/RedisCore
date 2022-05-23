#if !NETCOREAPP3_1_OR_GREATER
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore;

public static class SocketExtensions
{
    public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            return await SocketTaskExtensions.ReceiveAsync(socket, segment, socketFlags).ConfigureAwait(false);

        using var rentedBuffer = new RentedBuffer<byte>(buffer.Length);
        var result = await SocketTaskExtensions.ReceiveAsync(socket, rentedBuffer.Segment, socketFlags).ConfigureAwait(false);
        rentedBuffer.Memory.CopyTo(buffer);
        return result;
    }

    public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
            return await SocketTaskExtensions.SendAsync(socket, segment, socketFlags).ConfigureAwait(false);

        using var rentedBuffer = new RentedBuffer<byte>(buffer.Length);
        buffer.CopyTo(rentedBuffer.Memory);
        return await SocketTaskExtensions.SendAsync(socket, rentedBuffer.Segment, socketFlags).ConfigureAwait(false);
    }
}
#endif