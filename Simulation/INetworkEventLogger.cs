namespace AdHocMAC.Simulation
{
    interface INetworkEventLogger<T>
    {
        public void BeginReceive(T Receiver, T Sender, (byte, byte, byte) Colour);

        public void EndReceive(T Receiver, T Sender);

        public void BeginSend(T Sender, (byte, byte, byte) Colour);

        public void EndSend(T Sender);
    }
}
