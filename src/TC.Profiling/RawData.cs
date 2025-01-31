﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
			this.Samples = new RawSample[maxRawSamples];
			this.NextSampleIndex = 0;
			this.RootNode = null;
		}

		public void Clear()
		{
			this.NextSampleIndex = 0;
			this.RootNode = null;
		}

		public Stack<string> LabelStack = new Stack<string>();
		public Stack<RawNode> NodeStack = new Stack<RawNode>();
		public Stopwatch Stopwatch = new Stopwatch();

	}

}
