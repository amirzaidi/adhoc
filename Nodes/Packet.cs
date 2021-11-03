namespace AdHocMAC.Nodes
{
    struct Packet
    {
        // We need a sequence number to check ACKs.
        public int From, To, Seq;
        public string Data;

        public static int GetLength(Packet Packet)
        {
            var byteCount = 3 * sizeof(int) + Packet.Data.Length;
            return byteCount;
        }
    }
}
