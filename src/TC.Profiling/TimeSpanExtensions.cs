using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	internal static class TimeSpanExtensions
	{

		public static TimeSpan Min(TimeSpan a, TimeSpan b)
		{
			return a < b ? a : b;
		}

		public static TimeSpan Max(TimeSpan a, TimeSpan b)
		{
			return a > b ? a : b;
		}

	}

}
