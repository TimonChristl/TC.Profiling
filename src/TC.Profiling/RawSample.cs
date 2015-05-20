using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	internal struct RawSample
	{

		public DateTime StartTimestamp;
		public DateTime EndTimestamp;
		public long StartTicks;
		public long EndTicks;

	}

}
