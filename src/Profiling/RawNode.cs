using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Profiling
{

	internal class RawNode
	{

		private static int nextId = 0;
		public int Id = nextId++;

		public string Label = string.Empty;
		public List<RawNode> Children = new List<RawNode>();
		public List<int> SampleIndices = new List<int>();

	}

}
