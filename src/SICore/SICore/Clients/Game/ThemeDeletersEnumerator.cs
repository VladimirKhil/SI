using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICore.Clients.Game
{
	/// <summary>
	/// Перечислитель игроков, убирающих финальные темы
	/// </summary>
	public sealed class ThemeDeletersEnumerator
    {
		public sealed class IndexInfo
		{
			public int PlayerIndex { get; set; }
			public List<int> PossibleIndicies { get; private set; }

			public IndexInfo(int index)
			{
				if (index < 0)
					throw new ArgumentException("Недопустимый индекс", nameof(index));

				PlayerIndex = index;
			}

			public IndexInfo(List<int> indicies)
			{
				PlayerIndex = -1;
				PossibleIndicies = indicies; // Разделяют общий список
			}

			public void SetIndex(int index)
			{
				if (PlayerIndex != -1)
					throw new InvalidOperationException("Индекс уже задан!");

				if (!PossibleIndicies.Contains(index))
					throw new InvalidOperationException("Недопустимый индекс!");

				PlayerIndex = index;
				PossibleIndicies.Remove(index);
			}
		}

		/// <summary>
		/// Порядок объявления ставок
		/// </summary>
		private IndexInfo[] _order;

		/// <summary>
		/// Индекс в order
		/// </summary>
		private int _orderIndex;

		/// <summary>
		/// Является ли перебор циклическим (с конца идём в начало)
		/// </summary>
		private bool _cycle;

		/// <summary>
		/// Текущий элемент
		/// </summary>
		public IndexInfo Current => _order[_orderIndex];

		public ThemeDeletersEnumerator(IList<GamePlayerAccount> players, int themesCount)
		{
			var playersCount = players.Count;

			// Убираем так, чтобы лидер по сумме убирал тему последним
			var goodPlayers = players.Where(p => p.InGame).ToArray();
			var deletersCount = goodPlayers.Length;
			_order = new IndexInfo[deletersCount];

			var leftThemesCount = (themesCount - 1) % deletersCount;

			if (leftThemesCount == 0)
			{
				leftThemesCount = deletersCount;
			}

			// Классы игроков по суммам			
			var levels = goodPlayers.GroupBy(p => p.Sum).OrderByDescending(g => g.Key).ToArray();
			var k = leftThemesCount - 1;
			for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
			{
				var level = levels[levelIndex];
				if (level.Count() == 1)
				{
					_order[k--] = new IndexInfo(players.IndexOf(level.First()));
					if (k == -1)
					{
						k += deletersCount;
					}
				}
				else
				{
					var indicies = new List<int>();
					foreach (var item in level)
					{
						indicies.Add(players.IndexOf(item));
						_order[k--] = new IndexInfo(indicies);
						if (k == -1)
						{
							k += deletersCount;
						}
					}
				}
			}
		}

		private readonly List<string> _removeLog = new List<string>(); // Temp for catch errors

		public string GetRemoveLog() => string.Join(Environment.NewLine, _removeLog);

		public void RemoveAt(int index)
		{
			var removeLog = new StringBuilder(ToString()).Append(' ').Append(index);

			_removeLog.Add("Before: " + removeLog.ToString());

			if (!_order.Any(o => o.PlayerIndex == index))
			{
				for (var i = 0; i < _order.Length; i++)
				{
					if (_order[i].PlayerIndex > index)
					{
						_order[i].PlayerIndex--;
					}
				}

				return;
			}

			try
			{
				var newOrder = new IndexInfo[_order.Length - 1];

				for (int i = 0, j = 0; i < _order.Length; i++)
				{
					if (_order[i].PlayerIndex != index)
					{
						newOrder[j] = _order[i];

						if (_order[i].PlayerIndex > index)
						{
							newOrder[j].PlayerIndex--;
						}

						j++;
						if (j == newOrder.Length)
						{
							break;
						}
					}
				}

				if (_orderIndex == _order.Length - 1)
				{
					_orderIndex = _cycle ? 0 : newOrder.Length - 1;
				}

				_order = newOrder;

				_removeLog.Add("After: " + ToString());
			}
			catch (Exception exc)
			{
				throw new Exception("RemoveAt error: " + removeLog.ToString(), exc);
			}
		}

		public void Reset(bool cycle)
		{
			_orderIndex = -1;
			_cycle = cycle;
		}

		public bool MoveNext()
		{
			_orderIndex++;
			if (_orderIndex == _order.Length)
			{
				if (!_cycle || _order.Length == 0)
				{
					return false;
				}

				_orderIndex = 0;
			}

			return true;
		}

		public void MoveBack() => _orderIndex--;

		public override string ToString() => new StringBuilder(string.Join(",", _order.Select(o => o.PlayerIndex)))
				.Append(' ').Append(_orderIndex).Append(' ').Append(_cycle).ToString();
	}
}
