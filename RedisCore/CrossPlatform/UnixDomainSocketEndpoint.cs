// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedisCore
{
    public sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private const int NativePathOffset = 2; // sizeof(sun_family)
        private const int NativePathLength = 108; // sizeof(sun_path)
        private const int NativeAddressSize = NativePathOffset + NativePathLength; // sizeof(sockaddr_un)

        private static readonly Encoding PathEncoding = Encoding.UTF8;
        private static readonly Lazy<bool> UdsSupported = new Lazy<bool>(() =>
        {
            try
            {
                new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified).Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        });

        private readonly string _path;
        private readonly byte[] _encodedPath;

        public override AddressFamily AddressFamily => EndPointAddressFamily;

        public UnixDomainSocketEndPoint(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // Pathname socket addresses should be null-terminated.
            // Linux abstract socket addresses start with a zero byte, they must not be null-terminated.
            var isAbstract = IsAbstract(path);
            var bufferLength = PathEncoding.GetByteCount(path);
            if (!isAbstract)
            {
                // for null terminator
                bufferLength++;
            }

            if (path.Length == 0 || bufferLength > NativePathLength)
                throw new ArgumentOutOfRangeException(nameof(path), path, $"Path length should be less then or equal to {NativePathLength}");

            _path = path;
            _encodedPath = new byte[bufferLength];
            var bytesEncoded = PathEncoding.GetBytes(path, 0, path.Length, _encodedPath, 0);
            Debug.Assert(bufferLength - (isAbstract ? 0 : 1) == bytesEncoded);
            if (!UdsSupported.Value)
                throw new PlatformNotSupportedException();
        }

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            if (socketAddress == null)
                throw new ArgumentNullException(nameof(socketAddress));

            if (socketAddress.Family != EndPointAddressFamily || socketAddress.Size > NativeAddressSize)
                throw new ArgumentOutOfRangeException(nameof(socketAddress));

            if (socketAddress.Size > NativePathOffset)
            {
                _encodedPath = new byte[socketAddress.Size - NativePathOffset];
                for (var i = 0; i < _encodedPath.Length; ++i)
                    _encodedPath[i] = socketAddress[NativePathOffset + i];

                // Strip trailing null of pathname socket addresses.
                var length = _encodedPath.Length;
                if (!IsAbstract(_encodedPath))
                {
                    // Since this isn't an abstract path, we're sure our first byte isn't 0.
                    while (_encodedPath[length - 1] == 0)
                        length--;
                }
                _path = PathEncoding.GetString(_encodedPath, 0, length);
            }
            else
            {
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        public override SocketAddress Serialize()
        {
            var result = new SocketAddress(AddressFamily.Unix, NativeAddressSize);
            for (var index = 0; index < _encodedPath.Length; index++)
                result[NativePathOffset + index] = _encodedPath[index];
            return result;
        }

        public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

        public override string ToString() => IsAbstract(_path) ? "@" + _path.Substring(1) : _path;

        private static bool IsAbstract(string path) => path.Length > 0 && path[0] == '\0';

        private static bool IsAbstract(byte[] encodedPath) => encodedPath.Length > 0 && encodedPath[0] == 0;
    }
}
#endif