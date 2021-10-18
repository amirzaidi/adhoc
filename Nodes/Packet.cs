namespace AdHocMAC.Nodes
{
    struct Packet
    {
        // We need a sequence number to check ACKs.
        public int From, To, Seq;
        public string Data;
    }
}
