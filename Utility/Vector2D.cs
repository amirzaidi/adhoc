using System;

namespace AdHocMAC.Utility
{
    struct Vector2D
    {
        public double X, Y;

        public Vector2D Mult(double Multiplier) => new Vector2D
        {
            X = X * Multiplier,
            Y = Y * Multiplier
        };

        public Vector2D Normalize() => Mult(1.0 / Magnitude());

        public double Magnitude() => Math.Sqrt(X * X + Y * Y);
    }
}
