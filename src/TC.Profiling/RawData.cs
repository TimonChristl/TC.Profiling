using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

#if NET8_0_OR_GREATER
#pragma warning disable IDE0290
#endif

namespace TC.Profiling
{

	internal class RawData
	{

		public RawSample[] Samples;
		public int NextSampleIndex;
#if NET8_0_OR_GREATER
        public RawNode? RootNode;
#else
		public RawNode RootNode;
#endif

        public RawData(int maxRawSamples)
		{
			Samples = new RawSample[maxRawSamples];
			NextSampleIndex = 0;
			RootNode = null;
		}

		public void Clear()
		{
			NextSampleIndex = 0;
			RootNode = null;
		}

#if NET8_0_OR_GREATER
        public Stack<string> LabelStack = [];
        public Stack<RawNode> NodeStack = [];
        public Stopwatch Stopwatch = new();
#else
		public Stack<string> LabelStack = new Stack<string>();
		public Stack<RawNode> NodeStack = new Stack<RawNode>();
		public Stopwatch Stopwatch = new Stopwatch();
#endif

    }

}
