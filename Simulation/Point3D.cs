using System;

namespace AdHocMAC.Simulation
{
    struct Point3D
    {
        public double X, Y, Z;

        public static double Distance(Point3D A, Point3D B)
        {
            var dX = B.X - A.X;
            var dY = B.Y - A.Y;
            var dZ = B.Z - A.Z;

            return Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
        }
    }
}
