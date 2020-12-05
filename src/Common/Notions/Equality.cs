using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Notions
{
    /// <summary>
    /// Класс, содержащий информацию о равенстве символов в двух строках
    /// </summary>
    internal sealed class Equality
    {
        private readonly List<Point[]> _equalsMy = new List<Point[]>();
        private readonly List<Point[]> _equalsInheriredLeft = new List<Point[]>();
        private readonly List<Point[]> _equalsInheriredTop = new List<Point[]>();

        internal void Append(Equality other, int i, int j)
        {
            foreach (var item in other._equalsMy)
            {
                var list = new List<Point>(item)
                {
                    new Point(i, j)
                };
                _equalsMy.Add(list.ToArray());
            }

            foreach (var item in other._equalsInheriredLeft)
            {
                var list = new List<Point>(item)
                {
                    new Point(i, j)
                };
                _equalsMy.Add(list.ToArray());
            }

            foreach (var item in other._equalsInheriredTop)
            {
                var list = new List<Point>(item)
                {
                    new Point(i, j)
                };
                _equalsMy.Add(list.ToArray());
            }

            if (_equalsMy.Count == 0)
            {
                var newList = new Point[1] { new Point(i, j) };
                _equalsMy.Add(newList);
            }
        }

        internal void JoinLeft(Equality other)
        {
            foreach (var item in other._equalsMy)
            {
                var list = new List<Point>(item);
                _equalsInheriredLeft.Add(list.ToArray());
            }

            foreach (var item in other._equalsInheriredLeft)
            {
                var list = new List<Point>(item);
                _equalsInheriredLeft.Add(list.ToArray());
            }

            foreach (var item in other._equalsInheriredTop)
            {
                var list = new List<Point>(item);
                _equalsInheriredLeft.Add(list.ToArray());
            }
        }

        internal void JoinTop(Equality other)
        {
            foreach (var item in other._equalsMy)
            {
                var list = new List<Point>(item);
                _equalsInheriredTop.Add(list.ToArray());
            }

            foreach (var item in other._equalsInheriredTop)
            {
                var list = new List<Point>(item);
                _equalsInheriredTop.Add(list.ToArray());
            }
        }

        internal Point[] BestEquality(string string1, string string2, StringManager.StringNorm norm)
        {
            int best = int.MaxValue, pos = 0, bestpos = -1;

            var all = new List<Point[]>(_equalsMy);
            all.AddRange(_equalsInheriredLeft);
            all.AddRange(_equalsInheriredTop);

            foreach (var item in all)
            {
                int curr = norm(string1, string2, item);
                if (curr < best)
                {
                    best = curr;
                    bestpos = pos;
                }
                pos++;
            }

            return bestpos > -1 ? all[bestpos] : System.Array.Empty<Point>();
        }

        internal string Best(string string1, string string2, StringManager.StringNorm norm)
        {
            var best = BestEquality(string1, string2, norm);

            return best != null ? SubString(string1, best) : null;
        }

        private static string SubString(string str, Point[] equals)
        {
            var result = new StringBuilder();
            foreach (var item in equals)
            {
                result.Append(str[item.X]);
            }
            return result.ToString();
        }

        internal void WriteAll(string string1, string string2, StringManager.StringNorm norm, TextWriter writer)
        {
            var all = new List<Point[]>(_equalsMy);
            all.AddRange(_equalsInheriredLeft);
            all.AddRange(_equalsInheriredTop);

            foreach (var item in all)
            {
                writer.Write(SubString(string1, item));
                writer.Write(" : ");
                writer.WriteLine(norm(string1, string2, item));
            }
        }

        internal void LeaveOnlyBest(string string1, string string2, StringManager.StringNorm norm)
        {
            var all = new List<Point[]>(_equalsMy);
            all.AddRange(_equalsInheriredLeft);
            all.AddRange(_equalsInheriredTop);

            if (all.Count < 2)
                return;
            int best = int.MaxValue, pos = 0, bestpos = -1;

            foreach (var item in all)
            {
                int curr = norm(string1, string2, item);
                if (curr < best)
                {
                    best = curr;
                    bestpos = pos;
                }
                pos++;
            }

            var bestEq = all[bestpos];

            var toAdd = (bestpos < _equalsMy.Count) ? _equalsMy : ((bestpos < _equalsMy.Count + _equalsInheriredLeft.Count) ? _equalsInheriredLeft : _equalsInheriredTop);

            _equalsMy.Clear();
            _equalsInheriredLeft.Clear();
            _equalsInheriredTop.Clear();

            toAdd.Add(bestEq);
        }
    }
}
