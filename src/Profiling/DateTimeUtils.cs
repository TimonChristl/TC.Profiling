using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging
{

	/// <summary>
	/// Utilities for <see cref="DateTime"/> structs.
	/// </summary>
	public static class DateTimeUtils
	{

		/// <summary>
		/// Returns the lower of two <see cref="DateTime"/> structs.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static DateTime Min(DateTime a, DateTime b)
		{
			return a < b ? a : b;
		}

		/// <summary>
		/// Returns the greater of two <see cref="DateTime"/> structs.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static DateTime Max(DateTime a, DateTime b)
		{
			return a > b ? a : b;
		}

	}

}
