namespace AdHocMAC.Nodes.Routing
{
    struct RoutingPacketData : Packet.IByteLength
    {
        public int From, To;
        public int[] Nodes;
        public string Data;

        public int GetLength()
        {
            var byteCount = 2 * sizeof(int)
                + Nodes.Length * sizeof(int)
                + Data.Length;

            return byteCount;
        }
    }
}
