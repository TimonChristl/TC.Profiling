﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	/// <summary>
	/// A single sample of profiling result data. A sample corresponds to a single pair of
	/// invocations of <see cref="TC.Profiling.Profiler.Begin(string)"/> and <see cref="TC.Profiling.Profiler.End()"/>,
	/// while a node can correspond to many such pairs (e.g. in loops).
	/// </summary>
	public class ResultSample
	{

		#region Private fields

		private readonly DateTime startTimestamp;
		private readonly DateTime endTimestamp;
		private readonly TimeSpan duration;
		private readonly long startTicks;
		private readonly long endTicks;
		private readonly long durationTicks;

		#endregion

		#region Constructors

		internal ResultSample()
		{
			startTimestamp = DateTime.MinValue;
			endTimestamp = DateTime.MinValue;
			duration = TimeSpan.Zero;
			startTicks = 0;
			endTicks = 0;
			durationTicks = 0;
		}

		internal ResultSample(DateTime startTimestamp, DateTime endTimestamp, TimeSpan duration, long startTicks, long endTicks, long durationTicks)
		{
			this.startTimestamp = startTimestamp;
			this.endTimestamp = endTimestamp;
			this.duration = duration;
			this.startTicks = startTicks;
			this.endTicks = endTicks;
			this.durationTicks = durationTicks;
		}

		#endregion

		#region Internal methods

		internal static void Serialize(BinaryWriter binaryWriter, ResultSample sample)
		{
			binaryWriter.Write(sample.StartTimestamp.Ticks);
			binaryWriter.Write(sample.EndTimestamp.Ticks);
			binaryWriter.Write(sample.Duration.Ticks);

			binaryWriter.Write(sample.StartTicks);
			binaryWriter.Write(sample.EndTicks);
			binaryWriter.Write(sample.DurationTicks);
		}

		internal static ResultSample Unserialize(BinaryReader binaryReader)
		{
			long startTimestamp = binaryReader.ReadInt64();
			long endTimestamp = binaryReader.ReadInt64();
			long duration = binaryReader.ReadInt64();

			long startTicks = binaryReader.ReadInt64();
			long endTicks = binaryReader.ReadInt64();
			long durationTicks = binaryReader.ReadInt64();

			return new ResultSample(
				new DateTime(startTimestamp),
				new DateTime(endTimestamp),
				new TimeSpan(duration),
				startTicks,
				endTicks,
				durationTicks
			);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Start as an UTC <see cref="DateTime"/>.
		/// </summary>
		public DateTime StartTimestamp
		{
			get { return startTimestamp; }
		}

		/// <summary>
		/// End as an UTC <see cref="DateTime"/>.
		/// </summary>
		public DateTime EndTimestamp
		{
			get { return endTimestamp; }
		}

		/// <summary>
		/// Duration as <see cref="TimeSpan"/>.
		/// </summary>
		public TimeSpan Duration
		{
			get { return duration; }
		}

        /// <summary>
        /// Start in TimeSpan ticks.
        /// </summary>
        public long StartTicks
		{
			get { return startTicks; }
		}

        /// <summary>
        /// End in TimeSpan ticks.
        /// </summary>
        public long EndTicks
		{
			get { return endTicks; }
		}

		/// <summary>
		/// Duration in TimeSpan ticks.
		/// </summary>
		public long DurationTicks
		{
			get { return durationTicks; }
		}

		#endregion

	}

}
