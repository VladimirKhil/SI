using System;

namespace SICore.Utils
{
    internal static class RandomExtensions
    {
        internal static double NextGaussian(this Random random, double mean, double stddev)
        {
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return y1 * stddev + mean;
        }
    }
}
