using System;
using System.Collections.Generic;

namespace AdHocMAC.Utility
{
    class PoissonDistribution
    {
        private const double EPSILON = 0.0001;

        private readonly double[] mValues;

        public PoissonDistribution(double lambda)
        {
            var list = new List<double>();

            int k = 0;
            double kFac = 1.0;
            double ePowMinLambda = Math.Exp(-lambda);
            do
            {
                var P = (Math.Pow(lambda, k) / kFac) * ePowMinLambda;
                list.Add(P);

                k += 1;
                kFac *= k;
            } while (list[list.Count - 1] > EPSILON);

            mValues = list.ToArray();
        }

        public int GetXForCumulativeProb(double P)
        {
            if (P < 0 || P > 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            double cumulativeP = 0.0;
            for (int i = 0; i < mValues.Length; i++)
            {
                cumulativeP += mValues[i];
                if (cumulativeP >= P)
                {
                    return i;
                }
            }

            return mValues.Length;
        }
    }
}
