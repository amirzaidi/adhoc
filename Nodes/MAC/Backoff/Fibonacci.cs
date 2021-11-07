namespace AdHocMAC.Nodes.MAC.Backoff
{
    class Fibonacci : IBackoff
    {
        protected readonly int mMax;
        protected int mFib1 = 1, mFib2 = 1;

        public Fibonacci(int Max)
        {
            mMax = Max;
        }

        public void Increase()
        {
            if (mFib2 < mMax)
            {
                var tmp = mFib2;
                mFib2 = mFib1 + mFib2;
                mFib1 = tmp;
            }
        }

        public virtual void Decrease()
        {
            if (mFib2 > 1)
            {
                var tmp = mFib1;
                mFib1 = mFib2 - mFib1;
                mFib2 = tmp;
            }
        }

        public int UpperBoundExcl()
        {
            return mFib2;
        }
    }
}
