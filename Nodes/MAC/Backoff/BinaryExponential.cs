namespace AdHocMAC.Nodes.MAC.Backoff
{
    class BinaryExponential : IBackoff
    {
        protected readonly int mMin, mMax, mFactor;
        protected int mValue;

        public BinaryExponential(int Min, int Max, int Factor = 2)
        {
            mMin = Min;
            mMax = Max;
            mFactor = Factor;

            mValue = Min;
        }

        public void Increase()
        {
            if (mValue < mMax)
            {
                mValue *= mFactor;
            }
        }

        public virtual void Decrease()
        {
            mValue = mMin;
        }

        public int UpperBoundExcl()
        {
            return mValue;
        }
    }
}
