using System;

namespace SICore
{
    /// <summary>
    /// Граница отрезка с указанием расположения отрезка относительно точки
    /// </summary>
    internal sealed class DirPoint
    {
        public DirPoint(double value, bool direction)
        {
            Direction = direction;
            Value = direction ? (int)Math.Ceiling(value / 100) * 100 : (int)Math.Floor(value / 100) * 100;
        }

        /// <summary>
        /// Значение точки
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// true, если отрезок находится правее точки
        /// false, если отрезок находится левее точки
        /// </summary>
        public bool Direction { get; }

        public new string ToString() => $"{Value} {Direction}";
    }
}
