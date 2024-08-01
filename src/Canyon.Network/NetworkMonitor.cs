namespace Canyon.Network
{
    /// <summary>
    ///     A monitor for the networking I/O. From COPS V6 Enhanced Edition.
    /// </summary>
    public sealed class NetworkMonitor
    {
        private long totalRecvBytes;
        private int totalRecvPackets;

        private long totalSentBytes;
        private int totalSentPackets;

        /// <summary>
        ///     The number of bytes received by the server.
        /// </summary>
        private int recvBytes;

        private int recvPackets;

        /// <summary>
        ///     The number of bytes sent by the server.
        /// </summary>
        private int sentBytes;

        private int sentPackets;

        public int PacketsSent => sentPackets;
        public int PacketsRecv => recvPackets;
        public int BytesSent => sentBytes;
        public int BytesRecv => recvBytes;
        public long TotalPacketsSent => totalSentPackets;
        public long TotalPacketsRecv => totalRecvPackets;
        public long TotalBytesSent => totalSentBytes;
        public long TotalBytesRecv => totalRecvBytes;

        /// <summary>
        ///     Called by the timer.
        /// </summary>
        public string UpdateStatsAsync(int interval)
        {
            double download = recvBytes / (double)interval * 8.0 / 1024.0;
            double upload = sentBytes / (double)interval * 8.0 / 1024.0;
            int sent = sentPackets;
            int recv = recvPackets;

            Interlocked.Exchange(ref recvBytes, 0);
            Interlocked.Exchange(ref sentBytes, 0);
            Interlocked.Exchange(ref recvPackets, 0);
            Interlocked.Exchange(ref sentPackets, 0);

            return $"Network(↑{upload:F2} kbps [{sent:0000}], ↓{download:F2} kbps [{recv:0000}])";
        }

        /// <summary>
        ///     Signal to the monitor that aLength bytes were sent.
        /// </summary>
        /// <param name="aLength">The number of bytes sent.</param>
        public void Send(int aLength)
        {
            Interlocked.Increment(ref sentPackets);
            Interlocked.Increment(ref totalSentPackets);
            Interlocked.Add(ref sentBytes, aLength);
            Interlocked.Add(ref totalSentBytes, aLength);
        }

        /// <summary>
        ///     Signal to the monitor that aLength bytes were received.
        /// </summary>
        /// <param name="aLength">The number of bytes received.</param>
        public void Receive(int aLength)
        {
            Interlocked.Increment(ref recvPackets);
            Interlocked.Increment(ref totalRecvPackets);
            Interlocked.Add(ref recvBytes, aLength);
            Interlocked.Add(ref totalRecvBytes, aLength);
        }
    }
}
