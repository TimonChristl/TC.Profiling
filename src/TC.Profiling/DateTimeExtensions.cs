using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	internal static class DateTimeExtensions
	{

		public static DateTime Min(DateTime a, DateTime b)
		{
			return a < b ? a : b;
		}

		public static DateTime Max(DateTime a, DateTime b)
		{
			return a > b ? a : b;
		}

	}

}
