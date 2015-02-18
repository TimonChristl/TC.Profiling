using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Profiling
{

	/// <summary>
	/// Statistics over the samples of a <see cref="ResultNode"/>.
	/// </summary>
	public sealed class ResultTotalSample : ResultSample
	{

		#region Private fields

		private TimeSpan averageDuration;
		private TimeSpan minDuration;
		private TimeSpan maxDuration;

		private long averageDurationTicks;
		private long minDurationTicks;
		private long maxDurationTicks;

		#endregion

		#region Constructors

		internal ResultTotalSample()
		{
			this.averageDuration = TimeSpan.Zero;
			this.minDuration = TimeSpan.Zero;
			this.maxDuration = TimeSpan.Zero;

			this.averageDurationTicks = 0;
			this.minDurationTicks = 0;
			this.maxDurationTicks = 0;
		}

		internal ResultTotalSample(DateTime startTimestamp, DateTime endTimestamp, TimeSpan duration, long startTicks, long endTicks, long durationTicks, TimeSpan averageDuration, TimeSpan minDuration, TimeSpan maxDuration, long averageDurationTicks, long minDurationTicks, long maxDurationTicks)
			: base(startTimestamp, endTimestamp, duration, startTicks, endTicks, durationTicks)
		{
			this.averageDuration = averageDuration;
			this.minDuration = minDuration;
			this.maxDuration = maxDuration;

			this.averageDurationTicks = averageDurationTicks;
			this.minDurationTicks = minDurationTicks;
			this.maxDurationTicks = maxDurationTicks;
		}

		#endregion

		#region Internal methods

		internal static void Serialize(BinaryWriter binaryWriter, ResultTotalSample resultTotalSample)
		{
			binaryWriter.Write(resultTotalSample.StartTimestamp.Ticks);
			binaryWriter.Write(resultTotalSample.EndTimestamp.Ticks);
			binaryWriter.Write(resultTotalSample.Duration.Ticks);

			binaryWriter.Write(resultTotalSample.StartTicks);
			binaryWriter.Write(resultTotalSample.EndTicks);
			binaryWriter.Write(resultTotalSample.DurationTicks);

			binaryWriter.Write(resultTotalSample.averageDuration.Ticks);
			binaryWriter.Write(resultTotalSample.minDuration.Ticks);
			binaryWriter.Write(resultTotalSample.maxDuration.Ticks);

			binaryWriter.Write(resultTotalSample.averageDurationTicks);
			binaryWriter.Write(resultTotalSample.minDurationTicks);
			binaryWriter.Write(resultTotalSample.maxDurationTicks);
		}

		internal static new ResultTotalSample Unserialize(BinaryReader binaryReader)
		{
			long startTimestamp = binaryReader.ReadInt64();
			long endTimestamp = binaryReader.ReadInt64();
			long duration = binaryReader.ReadInt64();

			long startTicks = binaryReader.ReadInt64();
			long endTicks = binaryReader.ReadInt64();
			long durationTicks = binaryReader.ReadInt64();

			long averageDuration = binaryReader.ReadInt64();
			long minDuration = binaryReader.ReadInt64();
			long maxDuration = binaryReader.ReadInt64();
			long averageDurationTicks = binaryReader.ReadInt64();
			long minDurationTicks = binaryReader.ReadInt64();
			long maxDurationTicks = binaryReader.ReadInt64();

			return new ResultTotalSample(
				new DateTime(startTimestamp),
				new DateTime(endTimestamp),
				new TimeSpan(duration),
				startTicks,
				endTicks,
				durationTicks,
				new TimeSpan(averageDuration),
				new TimeSpan(minDuration),
				new TimeSpan(maxDuration),
				averageDurationTicks,
				minDurationTicks,
				maxDurationTicks
			);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Average duration among samples as <see cref="TimeSpan"/>.
		/// </summary>
		public TimeSpan AverageDuration
		{
			get { return averageDuration; }
		}

		/// <summary>
		/// Minimum duration among samples as <see cref="TimeSpan"/>.
		/// </summary>
		public TimeSpan MinDuration
		{
			get { return minDuration; }
		}

		/// <summary>
		/// Maximum duration among samples as <see cref="TimeSpan"/>.
		/// </summary>
		public TimeSpan MaxDuration
		{
			get { return maxDuration; }
		}

		/// <summary>
		/// Average duration among samples in Stopwatch ticks.
		/// </summary>
		public long AverageDurationTicks
		{
			get { return averageDurationTicks; }
		}

		/// <summary>
		/// Minimum duration among samples in Stopwatch ticks.
		/// </summary>
		public long MinDurationTicks
		{
			get { return minDurationTicks; }
		}

		/// <summary>
		/// Maximum duration among samples in Stopwatch ticks.
		/// </summary>
		public long MaxDurationTicks
		{
			get { return maxDurationTicks; }
		}

		#endregion

	}

}
