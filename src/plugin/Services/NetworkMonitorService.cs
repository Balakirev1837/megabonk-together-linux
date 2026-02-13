using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace MegabonkTogether.Services
{
    public interface INetworkMonitorService
    {
        void RecordPacketSent(int bytes, string messageType);
        void RecordPacketReceived(int bytes, string messageType);
        void RecordLatency(uint connectionId, int latencyMs);
        void LogSnapshot();
        void Reset();
    }

    public class NetworkMonitorService : INetworkMonitorService
    {
        private readonly ManualLogSource logger;
        private readonly ConcurrentDictionary<string, long> bytesSentByType = new();
        private readonly ConcurrentDictionary<string, long> bytesReceivedByType = new();
        private readonly ConcurrentDictionary<string, int> packetsSentByType = new();
        private readonly ConcurrentDictionary<string, int> packetsReceivedByType = new();
        private readonly ConcurrentDictionary<uint, int> latencyByPeer = new();
        
        private long totalBytesSent;
        private long totalBytesReceived;
        private DateTime sessionStart = DateTime.UtcNow;
        private DateTime lastLogTime = DateTime.UtcNow;
        
        private const int LOG_INTERVAL_SECONDS = 10;

        public NetworkMonitorService(ManualLogSource logger)
        {
            this.logger = logger;
        }

        public void RecordPacketSent(int bytes, string messageType)
        {
            Interlocked.Add(ref totalBytesSent, bytes);
            packetsSentByType.AddOrUpdate(messageType, 1, (_, c) => c + 1);
            bytesSentByType.AddOrUpdate(messageType, bytes, (_, c) => c + bytes);
            MaybeLogSnapshot();
        }

        public void RecordPacketReceived(int bytes, string messageType)
        {
            Interlocked.Add(ref totalBytesReceived, bytes);
            packetsReceivedByType.AddOrUpdate(messageType, 1, (_, c) => c + 1);
            bytesReceivedByType.AddOrUpdate(messageType, bytes, (_, c) => c + bytes);
            MaybeLogSnapshot();
        }

        public void RecordLatency(uint connectionId, int latencyMs)
        {
            latencyByPeer[connectionId] = latencyMs;
        }

        private void MaybeLogSnapshot()
        {
            if ((DateTime.UtcNow - lastLogTime).TotalSeconds >= LOG_INTERVAL_SECONDS)
            {
                LogSnapshot();
                lastLogTime = DateTime.UtcNow;
            }
        }

        public void LogSnapshot()
        {
            var elapsed = (DateTime.UtcNow - sessionStart).TotalSeconds;
            if (elapsed < 1) return;

            var sb = new StringBuilder();
            sb.Append("[NET_TELEMETRY] ");
            sb.Append($"uptime={elapsed:F0}s ");
            sb.Append($"tx={totalBytesSent / 1024}KB ");
            sb.Append($"rx={totalBytesReceived / 1024}KB ");
            sb.Append($"tx_rate={totalBytesSent / elapsed / 1024:F1}KB/s ");
            sb.Append($"rx_rate={totalBytesReceived / elapsed / 1024:F1}KB/s ");
            
            if (latencyByPeer.Count > 0)
            {
                var latencies = string.Join(",", latencyByPeer.Values);
                sb.Append($"latency=[{latencies}]ms ");
            }
            
            sb.Append($"msg_types_tx={packetsSentByType.Count} ");
            sb.Append($"msg_types_rx={packetsReceivedByType.Count}");

            logger.LogInfo(sb.ToString());
        }

        public void Reset()
        {
            bytesSentByType.Clear();
            bytesReceivedByType.Clear();
            packetsSentByType.Clear();
            packetsReceivedByType.Clear();
            latencyByPeer.Clear();
            totalBytesSent = 0;
            totalBytesReceived = 0;
            sessionStart = DateTime.UtcNow;
            lastLogTime = DateTime.UtcNow;
        }
    }
}
