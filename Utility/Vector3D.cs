using System;

namespace AdHocMAC.Simulation
{
    struct Vector3D
    {
        public double X, Y, Z;

        public static Vector3D Create(double X, double Y)
        {
            return new Vector3D()
            {
                X = X,
                Y = Y,
                Z = 0
            };
        }

        public static double Distance(Vector3D A, Vector3D B)
        {
            var dX = B.X - A.X;
            var dY = B.Y - A.Y;
            var dZ = B.Z - A.Z;

            return Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
        }
    }
}
