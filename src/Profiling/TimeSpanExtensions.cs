using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging
{

	/// <summary>
	/// Utilities for <see cref="TimeSpan"/> structs.
	/// </summary>
	public static class TimeSpanUtils
	{

		/// <summary>
		/// Returns the lower of two <see cref="TimeSpan"/> structs.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TimeSpan Min(TimeSpan a, TimeSpan b)
		{
			return a < b ? a : b;
		}

		/// <summary>
		/// Returns the greater of two <see cref="TimeSpan"/> structs.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TimeSpan Max(TimeSpan a, TimeSpan b)
		{
			return a > b ? a : b;
		}

	}

}
