#if NETSTANDARD2_0
using System.Text;

namespace System.Net.Sockets;

internal sealed class UnixDomainSocketEndPoint : EndPoint
{
    private const int NativePathOffset = 2;
    private const int NativePathLength = 108;

    private readonly string _path;
    private readonly byte[] _encodedPath;

    public override AddressFamily AddressFamily => AddressFamily.Unix;

    public UnixDomainSocketEndPoint(string path)
    {
        var isAbstract = IsAbstract(path);
        var bufferLength = Encoding.UTF8.GetByteCount(path);
        if (!isAbstract)
            ++bufferLength;

        if (path.Length == 0 || bufferLength > NativePathLength)
            throw new ArgumentOutOfRangeException(nameof(path), path, $"Path length exceeds maximum supported length of {NativePathLength}");

        _path = path;
        _encodedPath = new byte[bufferLength];
        Encoding.UTF8.GetBytes(path, 0, path.Length, _encodedPath, 0);
    }

    private UnixDomainSocketEndPoint(ReadOnlySpan<byte> socketAddress)
    {
        if (socketAddress.Length > NativePathOffset)
        {
            _encodedPath = new byte[socketAddress.Length - NativePathOffset];
            for (var i = 0; i < _encodedPath.Length; ++i)
                _encodedPath[i] = socketAddress[NativePathOffset + i];

            var length = _encodedPath.Length;
            if (!IsAbstract(_encodedPath))
                while (_encodedPath[length - 1] == 0)
                    --length;
            _path = Encoding.UTF8.GetString(_encodedPath, 0, length);
        }
        else
        {
            _encodedPath = [];
            _path = string.Empty;
        }
    }

    public override SocketAddress Serialize()
    {
        var result = new SocketAddress(AddressFamily.Unix, NativePathOffset + _encodedPath.Length);
        for (var i = 0; i < _encodedPath.Length; i++)
            result[NativePathOffset + i] = _encodedPath[i];
        return result;
    }

    public override EndPoint Create(SocketAddress socketAddress)
    {
        Span<byte> buffer = stackalloc byte[socketAddress.Size];
        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = socketAddress[i];
        return new UnixDomainSocketEndPoint(buffer);
    }

    public override string ToString() => IsAbstract(_path) ? $"@{_path[1..]}" : _path;

    public override bool Equals(object? obj) => obj is UnixDomainSocketEndPoint ep && ep._path == _path;

    public override int GetHashCode() => _path.GetHashCode();

    private static bool IsAbstract(string path) => path.Length > 0 && path[0] == '\0';
    private static bool IsAbstract(byte[] encodedPath) => encodedPath.Length > 0 && encodedPath[0] == 0;
}
#endif