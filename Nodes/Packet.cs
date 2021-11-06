namespace AdHocMAC.Nodes
{
    struct Packet
    {
        // We need a sequence number to check ACKs.
        public int From, To, Seq;
        public bool ACK;
        public string Data;

        public static int GetLength(Packet Packet)
        {
            var byteCount = 3 * sizeof(int)
                + 1 * sizeof(bool)
                + Packet.Data.Length;

            return byteCount;
        }
    }
}
