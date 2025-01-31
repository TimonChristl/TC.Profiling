﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TC.Profiling;

#if NET8_0_OR_GREATER
#pragma warning disable IDE0074
#endif

namespace TC.Profiling
{

    /// <summary>
    /// A simple profiler that requires code to be manually instrumented, but has very low impact in return.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To profile, first call <see cref="Start"/>, which starts profiling. Surround regions of your code with calls to <see cref="Begin"/> and <see cref="End"/>. These calls must be
    /// properly balanced. Each pair of calls creates one sample, these samples are then aggregated into a tree structure based on how Begin/End pairs are nested. To end profiling, call <see cref="Stop"/>.
    /// Before profiling again, call <see cref="Clear"/>, otherwise measurements will not be meaningful.
    /// </para>
    /// <para>
    /// To reduce allocations while profiling to a minimum, samples are written to a preallocated array whose size is specified when constructing a Profiler.
    /// The default size is 1024 * 1024, which equals 32 MB of memory. Note that an inactive profiler does not allocate this memory and always returns immediately in all methods, so an inactive
    /// profiler is as cheap as it gets.
    /// </para>
    /// </remarks>
    public sealed class Profiler
    {

        #region Private fields

#if NET8_0_OR_GREATER
        private readonly RawData? rawData;
        private ResultData? resultData;
#else
		private readonly RawData rawData;
        private ResultData resultData;
#endif

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
            if(isEnabled)
                rawData = new RawData(maxSamples);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Clears all raw data and all result data in this profiler.
        /// This method does nothing for an inactive profiler.
        /// </summary>
        public void Clear()
        {
            if(rawData != null)
            {
                rawData.Clear();
                resultData = null;
            }
        }

        /// <summary>
        /// Starts collection of raw data by creating the root node.
        /// This method does nothing for an inactive profiler.
        /// </summary>
        public void Start()
        {
            if(rawData == null)
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
            if(rawData == null)
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
            if(rawData == null)
                return;

            DateTime timestamp = DateTime.UtcNow;
            long ticks = Profiler.StopwatchTicksToTimeSpanTicks(rawData.Stopwatch.ElapsedTicks);

            rawData.LabelStack.Push(label);

#if NET8_0_OR_GREATER
            RawNode? node = EnsureRawNodesForPath(rawData, rawData.LabelStack);
#else
			RawNode node = EnsureRawNodesForPath(rawData, rawData.LabelStack);
#endif

            if(node != null)
                rawData.NodeStack.Push(node);

            if(rawData.NextSampleIndex < rawData.Samples.Length)
            {
                rawData.Samples[rawData.NextSampleIndex].StartTimestamp = timestamp;
                rawData.Samples[rawData.NextSampleIndex].EndTimestamp = timestamp;
                rawData.Samples[rawData.NextSampleIndex].StartTicks = ticks;
                rawData.Samples[rawData.NextSampleIndex].EndTicks = ticks;

                node?.SampleIndices.Add(rawData.NextSampleIndex);

                rawData.NextSampleIndex++;
            }

            resultData = null;
        }

        /// <summary>
        /// Ends the most recently sample.
        /// This method does nothing for an inactive profiler.
        /// </summary>
        public void End()
        {
            if(rawData == null)
                return;

            RawNode node = rawData.NodeStack.Pop();

            DateTime timestamp = DateTime.UtcNow;
            long ticks = Profiler.StopwatchTicksToTimeSpanTicks(rawData.Stopwatch.ElapsedTicks);

            if(node.SampleIndices.Count > 0)
            {
#if NET8_0_OR_GREATER
                int sampleIndex = node.SampleIndices[^1];
#else
				int sampleIndex = node.SampleIndices[node.SampleIndices.Count - 1];
#endif

                rawData.Samples[sampleIndex].EndTimestamp = timestamp;
                rawData.Samples[sampleIndex].EndTicks = ticks;
            }

            rawData.LabelStack.Pop();

            resultData = null;
        }

        #endregion

        #region Private methods

        private static long StopwatchTicksToTimeSpanTicks(long stopwatchTicks)
        {
            return stopwatchTicks * (10 * 1000 * 1000) / Stopwatch.Frequency;
        }

#if NET8_0_OR_GREATER
        private static RawNode? EnsureRawNodesForPath(RawData rawData, IEnumerable<string> labelStack)
#else
		private static RawNode EnsureRawNodesForPath(RawData rawData, IEnumerable<string> labelStack)
#endif
        {
            var children = new List<RawNode>();
            if(rawData.RootNode != null)
                children.Add(rawData.RootNode);

#if NET8_0_OR_GREATER
            RawNode? currentNode = rawData.RootNode;
#else
			RawNode currentNode = rawData.RootNode;
#endif

            foreach(string label in labelStack.Reverse())
            {
                int childIndex = children.FindIndex(child => child.Label == label);

                if(childIndex == -1)
                {
                    var newChild = new RawNode { Label = label, };
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

        private static ResultNode MakeResultNode(RawData rawData, RawNode rawNode)
        {
            ResultNode[] children = new ResultNode[rawNode.Children.Count];

            for(int i = 0; i < rawNode.Children.Count; i++)
                children[i] = MakeResultNode(rawData, rawNode.Children[i]);

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

        private static ResultSample MakeResultSample(RawSample rawSample)
        {
            TimeSpan duration = rawSample.EndTimestamp - rawSample.StartTimestamp;
            long durationTicks = rawSample.EndTicks - rawSample.StartTicks;

            return new ResultSample(rawSample.StartTimestamp, rawSample.EndTimestamp, duration, rawSample.StartTicks, rawSample.EndTicks, durationTicks);
        }

        private static ResultTotalSample MakeResultTotalSample(ResultNode[] children, ResultSample[] samples)
        {
            DateTime startTimestamp = DateTime.MaxValue;
            DateTime endTimestamp = DateTime.MinValue;
            long startTicks = long.MaxValue;
            long endTicks = long.MinValue;

            foreach(ResultNode child in children)
            {
                startTimestamp = DateTimeExtensions.Min(startTimestamp, child.Total.StartTimestamp);
                endTimestamp = DateTimeExtensions.Max(endTimestamp, child.Total.EndTimestamp);
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
                    startTimestamp = DateTimeExtensions.Min(startTimestamp, sample.StartTimestamp);
                    endTimestamp = DateTimeExtensions.Max(endTimestamp, sample.EndTimestamp);
                    startTicks = Math.Min(startTicks, sample.StartTicks);
                    endTicks = Math.Max(endTicks, sample.EndTicks);

                    TimeSpan sampleDuration = sample.EndTimestamp - sample.StartTimestamp;
                    long sampleDurationTicks = sample.EndTicks - sample.StartTicks;

                    averageDuration += sampleDuration;
                    minDuration = TimeSpanExtensions.Min(minDuration, sampleDuration);
                    maxDuration = TimeSpanExtensions.Max(maxDuration, sampleDuration);

                    averageDurationTicks += sampleDurationTicks;
                    minDurationTicks = Math.Min(minDurationTicks, sampleDurationTicks);
                    maxDurationTicks = Math.Max(maxDurationTicks, sampleDurationTicks);
                }

                averageDuration = TimeSpan.FromTicks(averageDuration.Ticks / samples.Length);
                averageDurationTicks /= samples.Length;
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
        /// It is possible to get result data while profiling, but since calculating result data takes time it is recommended
        /// to only get results after profiling is finished in order to get more accurate results. Calculated result data
        /// is not recalculated until the next call to <see cref="Begin"/>, <see cref="End"/>, <see cref="Start"/>, <see cref="Stop"/>
        /// or <see cref="Clear"/>.
        /// </remarks>
#if NET8_0_OR_GREATER
        public ResultData? ResultData
#else
		public ResultData ResultData
#endif
        {
            get
            {
                if(resultData == null)
                    resultData = rawData?.RootNode != null
                        ? new ResultData(MakeResultNode(rawData, rawData.RootNode))
                        : new ResultData();

                return resultData;
            }
        }

        #endregion
    }
}
