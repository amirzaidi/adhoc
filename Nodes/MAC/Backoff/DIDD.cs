namespace AdHocMAC.Nodes.MAC.Backoff
{
    class DIDD : BinaryExponential
    {
        public DIDD(int Min, int Max, int Factor = 2) : base(Min, Max, Factor)
        {
        }

        public override void Decrease()
        {
            if (mValue > mMin)
            {
                mValue /= mFactor;
            }
        }
    }
}
