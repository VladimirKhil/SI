using System.Diagnostics.CodeAnalysis;

namespace Notions;

public struct Point
{
    public int X { get; set; }

    public int Y { get; set; }

    public Point(int x, int y) : this()
    {
        X = x;
        Y = y;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Point other)
        {
            return base.Equals(obj);
        }

        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Point left, Point right) => left.Equals(right);

    public static bool operator !=(Point left, Point right) => !(left == right);
}
