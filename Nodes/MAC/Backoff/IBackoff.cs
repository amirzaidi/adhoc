namespace AdHocMAC.Nodes.MAC.Backoff
{
    interface IBackoff
    {
        public void Increase();

        public void Decrease();

        public int UpperBoundExcl();
    }
}
