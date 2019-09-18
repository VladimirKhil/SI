using System;
using System.Collections.Generic;
using System.Text;

namespace Services.SI
{
	/// <summary>
	/// Именованный объект
	/// </summary>
    public sealed class NamedObject
    {
		public int ID { get; set; }

		/// <summary>
		/// Имя
		/// </summary>
		public string Name { get; set; }
	}
}
