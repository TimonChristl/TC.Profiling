using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Profiling;

namespace Logging
{

	/// <summary>
	/// A simple profiler that requires code to be manually instrumented, but has very low impact in return.
	/// </summary>
	/// <remarks>
	/// <para>
	/// To profile, call methods <see cref="Begin"/> and <see cref="End"/> in pairs.
	/// This creates samples, which are aggregated into nodes in a tree, based on the path of labels that results from nesting pairs of calls.
	/// </para>
	/// <para>
	/// To avoid many allocations while profiling, the samples are written to a preallocated array whose size is specified when constructing the Profiler.
	/// The default size is 1024 * 1024, which equals 32 MB of memory. This memory is re-used when clearing the profiler.
	/// </para>
	/// </remarks>
	public sealed class Profiler
	{

		#region Private fields

		private bool isEnabled;
		private int maxSamples;

		private RawData rawData;

		private ResultData resultData;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new <see cref="Profiler"/> and activates it based on whether <paramref name="isEnabled"/> is true,
		/// with the maximum number of samples set to <paramref name="maxSamples"/>.
		/// An inactive profiler consumes no extra memory and returns immediately in all methods.
		/// </summary>
		/// <param name="isEnabled"></param>
		/// <param name="maxSamples"></param>
		public Profiler(bool isEnabled = true, int maxSamples = 1024 * 1024)
		{
			this.isEnabled = isEnabled;
			this.maxSamples = maxSamples;

			Clear();
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Clears all raw data and all result data in this profiler.
		/// This method does nothing for an inactive profiler.
		/// </summary>
		public void Clear()
		{
			if(!isEnabled)
				return;

			if(this.rawData == null)
				this.rawData = new RawData(maxSamples);
			else
				this.rawData.Clear();

			this.resultData = null;
		}

		/// <summary>
		/// Starts collection of raw data by creating the root node.
		/// This method does nothing for an inactive profiler.
		/// </summary>
		public void Start()
		{
			if(!isEnabled)
				return;

			rawData.Stopwatch = Stopwatch.StartNew();

			Begin("<all>");
		}

		/// <summary>
		/// Ends collection of raw data.
		/// This method does nothing for an inactive profiler.
		/// </summary>
		public void Stop()
		{
			if(!isEnabled)
				return;

			End();

			rawData.Stopwatch.Stop();
		}

		/// <summary>
		/// Begins a sample with label <paramref name="label"/>.
		/// This method does nothing for an inactive profiler.
		/// </summary>
		/// <param name="label"></param>
		public void Begin(string label)
		{
			if(!isEnabled)
				return;

			DateTime timestamp = DateTime.UtcNow;
			long ticks = rawData.Stopwatch.ElapsedTicks;

			rawData.LabelStack.Push(label);

			RawNode node = EnsureRawNodesForPath(rawData.LabelStack);

			rawData.NodeStack.Push(node);

			rawData.Samples[rawData.NextSampleIndex].StartTimestamp = timestamp;
			rawData.Samples[rawData.NextSampleIndex].EndTimestamp = timestamp;
			rawData.Samples[rawData.NextSampleIndex].StartTicks = ticks;
			rawData.Samples[rawData.NextSampleIndex].EndTicks = ticks;

			node.SampleIndices.Add(rawData.NextSampleIndex);

			rawData.NextSampleIndex++;

			resultData = null;
		}

		/// <summary>
		/// Ends the most recently sample.
		/// This method does nothing for an inactive profiler.
		/// </summary>
		public void End()
		{
			if(!isEnabled)
				return;

			RawNode node = rawData.NodeStack.Pop();

			DateTime timestamp = DateTime.UtcNow;
			long ticks = rawData.Stopwatch.ElapsedTicks;

			int sampleIndex = node.SampleIndices[node.SampleIndices.Count - 1];

			rawData.Samples[sampleIndex].EndTimestamp = timestamp;
			rawData.Samples[sampleIndex].EndTicks = ticks;

			rawData.LabelStack.Pop();

			resultData = null;
		}

		#endregion

		#region Private methods

		private RawNode EnsureRawNodesForPath(IEnumerable<string> labelStack)
		{
			List<RawNode> children = new List<RawNode>();
			if(rawData.RootNode != null)
				children.Add(rawData.RootNode);
			RawNode currentNode = rawData.RootNode;

			foreach(string label in labelStack.Reverse())
			{
				int childIndex = children.FindIndex(child => child.Label == label);

				if(childIndex == -1)
				{
					RawNode newChild = new RawNode { Label = label, };
					children.Add(newChild);

					if(rawData.RootNode == null)
						rawData.RootNode = newChild;

					currentNode = newChild;
				}
				else
					currentNode = children[childIndex];

				children = currentNode.Children;
			}

			return currentNode;
		}

		private void MakeResultData()
		{
			this.resultData = this.rawData != null
				? new ResultData(MakeResultNode(this.rawData.RootNode))
				: new ResultData();
		}

		private ResultNode MakeResultNode(RawNode rawNode)
		{
			ResultNode[] children = new ResultNode[rawNode.Children.Count];

			for(int i = 0; i < rawNode.Children.Count; i++)
				children[i] = MakeResultNode(rawNode.Children[i]);

			ResultSample[] samples = new ResultSample[rawNode.SampleIndices.Count];

			for(int i = 0; i < rawNode.SampleIndices.Count; i++)
				samples[i] = MakeResultSample(rawData.Samples[rawNode.SampleIndices[i]]);

			ResultTotalSample total = MakeResultTotalSample(children, samples);

			return new ResultNode(
				rawNode.Id,
				rawNode.Label,
				total,
				samples,
				children
			);
		}

		private ResultSample MakeResultSample(RawSample rawSample)
		{
			TimeSpan duration = rawSample.EndTimestamp - rawSample.StartTimestamp;
			long durationTicks = rawSample.EndTicks - rawSample.StartTicks;

			return new ResultSample(rawSample.StartTimestamp, rawSample.EndTimestamp, duration, rawSample.StartTicks, rawSample.EndTicks, durationTicks);
		}

		private ResultTotalSample MakeResultTotalSample(ResultNode[] children, ResultSample[] samples)
		{
			DateTime startTimestamp = DateTime.MaxValue;
			DateTime endTimestamp = DateTime.MinValue;
			long startTicks = long.MaxValue;
			long endTicks = long.MinValue;

			foreach(ResultNode child in children)
			{
				startTimestamp = DateTimeUtils.Min(startTimestamp, child.Total.StartTimestamp);
				endTimestamp = DateTimeUtils.Max(endTimestamp, child.Total.EndTimestamp);
				startTicks = Math.Min(startTicks, child.Total.StartTicks);
				endTicks = Math.Max(endTicks, child.Total.EndTicks);
			}

			TimeSpan averageDuration = TimeSpan.Zero;
			TimeSpan minDuration = TimeSpan.MaxValue;
			TimeSpan maxDuration = TimeSpan.MinValue;
			long averageDurationTicks = 0;
			long minDurationTicks = long.MaxValue;
			long maxDurationTicks = long.MinValue;

			if(samples.Length > 0)
			{
				foreach(ResultSample sample in samples)
				{
					startTimestamp = DateTimeUtils.Min(startTimestamp, sample.StartTimestamp);
					endTimestamp = DateTimeUtils.Max(endTimestamp, sample.EndTimestamp);
					startTicks = Math.Min(startTicks, sample.StartTicks);
					endTicks = Math.Max(endTicks, sample.EndTicks);

					TimeSpan sampleDuration = sample.EndTimestamp - sample.StartTimestamp;
					long sampleDurationTicks = sample.EndTicks - sample.StartTicks;

					averageDuration += sampleDuration;
					minDuration = TimeSpanUtils.Min(minDuration, sampleDuration);
					maxDuration = TimeSpanUtils.Max(maxDuration, sampleDuration);

					averageDurationTicks += sampleDurationTicks;
					minDurationTicks = Math.Min(minDurationTicks, sampleDurationTicks);
					maxDurationTicks = Math.Max(maxDurationTicks, sampleDurationTicks);
				}

				averageDuration = TimeSpan.FromTicks(averageDuration.Ticks / samples.Length);
				averageDurationTicks = averageDurationTicks / samples.Length;
			}
			else
			{
				averageDuration = TimeSpan.Zero;
				minDuration = TimeSpan.Zero;
				maxDuration = TimeSpan.Zero;
				averageDurationTicks = 0;
				minDurationTicks = 0;
				maxDurationTicks = 0;
			}

			TimeSpan duration = endTimestamp - startTimestamp;
			long durationTicks = endTicks - startTicks;

			return new ResultTotalSample(startTimestamp, endTimestamp, duration, startTicks, endTicks, durationTicks, averageDuration, minDuration, maxDuration, averageDurationTicks, minDurationTicks, maxDurationTicks);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Returns result data. Reading this property the first time takes longer due to the result data being calculated.
		/// </summary>
		/// <remarks>
		/// It is best to only read from this property after profiling, since calculating results can take some time.
		/// </remarks>
		public ResultData ResultData
		{
			get
			{
				if(resultData == null)
					MakeResultData();

				return resultData;
			}
		}

		#endregion

	}

}
