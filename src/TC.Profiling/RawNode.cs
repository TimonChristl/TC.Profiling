using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	internal class RawNode
	{

		private static int nextId = 0;
		public int Id = nextId++;

		public string Label = string.Empty;
#if NET8_0_OR_GREATER
		public List<RawNode> Children = [];
        public List<int> SampleIndices = [];
#else
		public List<RawNode> Children = new List<RawNode>();
		public List<int> SampleIndices = new List<int>();
#endif

    }

}
