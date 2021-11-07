namespace AdHocMAC.Nodes
{
    struct Packet
    {
        public const int BROADCAST_TO_ID = -1;

        // We need a sequence number to check ACKs.
        public int From, To, Seq;
        public bool ACK;
        public object Data;

        // Only for logging.
        public long InitialUnixTimestamp;
        public int RetryAttempts;

        public static int GetLength(Packet Packet)
        {
            var byteCount = 3 * sizeof(int)
                + 1 * sizeof(PacketType);

            if (Packet.Data is string dataStr)
            {
                byteCount += dataStr.Length;
            }
            else if (Packet.Data is IByteLength ib)
            {
                byteCount += ib.GetLength();
            }

            return byteCount;
        }

        public interface IByteLength
        {
            public int GetLength();
        }
    }
}
