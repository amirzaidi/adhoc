namespace AdHocMAC.Nodes.Routing
{
    struct RoutingPacketControl : Packet.IByteLength
    {
        public int From, To, Seq;
        public int[] Nodes;
        public bool Reply;

        public int GetLength()
        {
            var byteCount = 3 * sizeof(int)
                + Nodes.Length * sizeof(int)
                + sizeof(bool);

            return byteCount;
        }
    }
}
