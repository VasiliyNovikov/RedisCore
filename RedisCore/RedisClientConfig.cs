using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedisCore
{
    public class RedisClientConfig
    {
        private const int DefaultTcpPort = 6379;
        private const int DefaultSslPort = 6380;
        private const int DefaultBufferSize = 2048;
        private const int DefaultMaxFreeConnections = 2;

        private static int DefaultPort(bool useSsl) => useSsl ? DefaultSslPort : DefaultTcpPort;
        
        
        public EndPoint EndPoint { get; }
        
        public bool UseSsl { get; }

        public string HostName{ get; }
        
        public string Password { get; set; }

        public int BufferSize { get; set; } = DefaultBufferSize;

        public int MaxFreeConnections { get; set; } = DefaultMaxFreeConnections;

        public bool ForceUseNetworkStream { get; set; }

        public TimeSpan LoadingRetryDelayMin { get; set; } = TimeSpan.FromMilliseconds(20);
        
        public TimeSpan LoadingRetryDelayMax { get; set; } = TimeSpan.FromMilliseconds(200);
        
        public TimeSpan LoadingRetryTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public RedisClientConfig(EndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        public RedisClientConfig(string address, bool useSsl = false)
        {
            UseSsl = useSsl;
            var addressParts = address.Split(':');
            var hostStr = addressParts[0];
            var port = addressParts.Length < 2 ? DefaultPort(useSsl) : Int32.Parse(addressParts[1]);
            if (!IPAddress.TryParse(hostStr, out var hostAddress))
            {
                HostName = hostStr;
                hostAddress = Dns.GetHostEntry(hostStr).AddressList[0];
            }

            EndPoint = new IPEndPoint(hostAddress, port);
        }

        public override string ToString()
        {
            var schema = EndPoint is UnixDomainSocketEndPoint ? "unix" : (UseSsl ? "ssl" : "tcp");
            string address;
            if (EndPoint is UnixDomainSocketEndPoint)
                address = EndPoint.ToString();
            else
            {
                var ipEndPoint = (IPEndPoint) EndPoint;
                var host = HostName ?? ipEndPoint.Address.ToString();
                address = ipEndPoint.Port == DefaultPort(UseSsl) ? host : $"{host}:{ipEndPoint.Port}";
            }

            var featureBuilder = new StringBuilder();
            if (BufferSize != DefaultBufferSize)
                featureBuilder.Append($"bufferSize={BufferSize}");
            if (MaxFreeConnections != DefaultMaxFreeConnections)
            {
                if (featureBuilder.Length > 0)
                    featureBuilder.Append(", ");
                featureBuilder.Append($"maxFreeConnections={MaxFreeConnections}");
            }
            if (ForceUseNetworkStream)
            {
                if (featureBuilder.Length > 0)
                    featureBuilder.Append(", ");
                featureBuilder.Append("forceUseNetworkStream");
            }

            return $"{schema}://{address} {{{featureBuilder}}})";
        }
    }
}