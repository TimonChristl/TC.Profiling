using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Logging;

namespace Profiling
{

	/// <summary>
	/// Contains processed profiling result data and can output this data in several formats.
	/// </summary>
	public sealed class ResultData
	{

		#region Private fields

		private ResultNode rootNode;

		private static readonly Guid binaryFileV1Marker = new Guid("EE1F4A3C-1C03-4A27-A83B-ED77A02A59FE");

		private static readonly Dictionary<IndentStyle, string[]> indentChars = new Dictionary<IndentStyle, string[]>()
		{
			{
				IndentStyle.Unicode,
				new string[] { "└─ ", "├─ ", "   ", "│  " }
			},
			{
				IndentStyle.Ascii,
				new string[] { "`- ", "+- ", "   ", "|  " }
			},
		};

		#endregion

		#region Constructors

		internal ResultData()
		{
			this.rootNode = new ResultNode();
		}

		internal ResultData(ResultNode rootNode)
		{
			this.rootNode = rootNode;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Writes result data in aggregated text format to a file.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="indentStyle"></param>
		public void ToFileAsText(string filename, IndentStyle indentStyle = IndentStyle.Unicode)
		{
			using(FileStream stream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
			using(StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8))
			{
				Serialize((line) => streamWriter.WriteLine(line), indentStyle);
			}
		}

		/// <summary>
		/// Writes result data in aggregated text format to a <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="indentStyle"></param>
		public void ToStreamAsText(Stream stream, IndentStyle indentStyle = IndentStyle.Unicode)
		{
			using(StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
			{
				Serialize((line) => streamWriter.WriteLine(line), indentStyle);
			}
		}

		/// <summary>
		/// Writes result data in aggregated text format to a <see cref="StreamWriter"/>.
		/// </summary>
		/// <param name="streamWriter"></param>
		/// <param name="indentStyle"></param>
		public void ToStreamWriterAsText(StreamWriter streamWriter, IndentStyle indentStyle = IndentStyle.Unicode)
		{
			Serialize((line) => streamWriter.WriteLine(line), indentStyle);
		}

		/// <summary>
		/// Writes result data in binary format to a file.
		/// </summary>
		/// <param name="filename"></param>
		public void ToFileAsBinary(string filename)
		{
			using(FileStream stream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
			using(BinaryWriter binaryWriter = new BinaryWriter(stream))
			{
				SerializeBinary(binaryWriter);
			}
		}

		/// <summary>
		/// Writes result data in binary format to a <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream"></param>
		public void ToStreamAsBinary(Stream stream)
		{
			using(BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true))
			{
				SerializeBinary(binaryWriter);
			}
		}

		/// <summary>
		/// Reads result data in binary format from a file.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static ResultData FromFileAsBinary(string filename)
		{
			using(FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			using(BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8))
			{
				return ResultData.UnserializeBinary(binaryReader);
			}
		}

		/// <summary>
		/// Reads result data in binary format from a <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public static ResultData FromStreamAsBinary(Stream stream)
		{
			using(BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
			{
				return ResultData.UnserializeBinary(binaryReader);
			}
		}

		/// <summary>
		/// Writes result data in aggregated text format to a <see cref="Logger"/>.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="severity"></param>
		/// <param name="logger"></param>
		/// <param name="indentStyle"></param>
		public void ToLogger(string label, Severity severity, Logger logger, IndentStyle indentStyle = IndentStyle.Unicode)
		{
			StringBuilder sb = new StringBuilder();

			Serialize((line) => sb.AppendLine(line), indentStyle);

			logger.Log(severity, label, sb.ToString());
		}

		/// <summary>
		/// Writes result data in aggregated text format to a string with indentation style <see cref="IndentStyle.Unicode"/>.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToString(IndentStyle.Unicode);
		}

		/// <summary>
		/// Writes result data in aggregated text format to a string with indentation style <paramref name="indentStyle"/>.
		/// </summary>
		/// <param name="indentStyle"></param>
		/// <returns></returns>
		public string ToString(IndentStyle indentStyle)
		{
			StringBuilder sb = new StringBuilder();

			Serialize((line) => sb.AppendLine(line), indentStyle);

			return sb.ToString();
		}

		/// <summary>
		/// Writes result data in JSON format to a string.
		/// </summary>
		/// <returns></returns>
		public string ToJson()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.AppendLine();

			sb.Append("    \"Root\" :");
			sb.AppendLine();

			int level = 1;

			Traverse(
				(node, index, count) =>
				{
					string indentation = string.Empty.PadRight(4 * level, ' ');

					sb.Append(indentation);
					sb.Append("{");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    \"Id\" : ");
					sb.Append(node.Id);
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    \"Label\" : \"");
					sb.Append(node.Label);
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    \"Samples\" :");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    [");
					sb.AppendLine();

					for(int i = 0; i < node.Samples.Length; i++)
					{
						ResultSample sample = node.Samples[i];

						sb.Append(indentation);
						sb.Append("        {");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"Duration\" : \"");
						sb.Append(FormatTimeSpanForJson(sample.Duration));
						sb.Append("\",");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"DurationTicks\" : ");
						sb.Append(sample.DurationTicks.ToString(NumberFormatInfo.InvariantInfo));
						sb.Append(",");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"EndTicks\" : ");
						sb.Append(sample.EndTicks.ToString(NumberFormatInfo.InvariantInfo));
						sb.Append(",");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"EndTimestamp\" : \"");
						sb.Append(FormatDateTimeForJson(sample.EndTimestamp));
						sb.Append("\",");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"StartTicks\" : ");
						sb.Append(sample.StartTicks.ToString(NumberFormatInfo.InvariantInfo));
						sb.Append(",");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("            \"StartTimestamp\" : \"");
						sb.Append(FormatDateTimeForJson(sample.StartTimestamp));
						sb.Append("\"");
						sb.AppendLine();

						sb.Append(indentation);
						sb.Append("        }");
						if(i < node.Samples.Length - 1)
							sb.Append(",");
						sb.AppendLine();
					}

					sb.Append(indentation);
					sb.Append("    ],");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    \"Total\" : {");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"AverageDuration\" : \"");
					sb.Append(FormatTimeSpanForJson(node.Total.AverageDuration));
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"AverageDurationTicks\" : ");
					sb.Append(node.Total.AverageDurationTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"Duration\" : \"");
					sb.Append(FormatTimeSpanForJson(node.Total.MinDuration));
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"DurationTicks\" : ");
					sb.Append(node.Total.DurationTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"EndTicks\" : ");
					sb.Append(node.Total.EndTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"EndTimestamp\" : \"");
					sb.Append(FormatDateTimeForJson(node.Total.EndTimestamp));
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"MaxDuration\" : \"");
					sb.Append(FormatTimeSpanForJson(node.Total.MaxDuration));
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"MaxDurationTicks\" : ");
					sb.Append(node.Total.MaxDurationTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"MinDuration\" : \"");
					sb.Append(FormatTimeSpanForJson(node.Total.MinDuration));
					sb.Append("\",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"MinDurationTicks\" : ");
					sb.Append(node.Total.MinDurationTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"StartTicks\" : ");
					sb.Append(node.Total.StartTicks.ToString(NumberFormatInfo.InvariantInfo));
					sb.Append(",");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("        \"StartTimestamp\" : \"");
					sb.Append(FormatDateTimeForJson(node.Total.StartTimestamp));
					sb.Append("\"");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    },");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    \"Children\" :");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("    [");
					sb.AppendLine();

					level += 2;
				},
				(node, index, count) =>
				{
					level -= 2;

					string indentation = string.Empty.PadRight(4 * level, ' ');

					sb.Append(indentation);
					sb.Append("    ]");
					sb.AppendLine();

					sb.Append(indentation);
					sb.Append("}");
					if(index < count - 1)
						sb.Append(",");
					sb.AppendLine();
				}
			);

			sb.AppendLine("}");

			return sb.ToString();
		}

		#endregion

		#region Private methods

		private void Serialize(Action<string> lineAction, IndentStyle indentStyle)
		{
			#region Determine max level and max label length

			int level = 0;

			int totalMaxLevel = 0;
			int totalMaxLabelLength = 0;

			Traverse((node, index, count) =>
				{
					level++;

					totalMaxLevel = Math.Max(totalMaxLevel, level);
					totalMaxLabelLength = Math.Max(totalMaxLabelLength, node.Label.Length);
				},
				(node, index, count) =>
				{
					level--;
				}
			);

			#endregion

			#region Write header

			const int indentWidth = 3;
			const int millisecondsWidth = 8;
			const int microsecondsWidth = 12;
			const int hitCountWidth = 5;
			const int percentageWidth = 7;

			int outputWidth = totalMaxLevel * indentWidth + totalMaxLabelLength
				+ 2
				+ millisecondsWidth
				+ 2
				+ microsecondsWidth
				+ 2
				+ percentageWidth
				+ 2
				+ hitCountWidth
				+ 2
				+ millisecondsWidth
				+ 2
				+ microsecondsWidth
				+ 2
				+ millisecondsWidth
				+ 2
				+ microsecondsWidth
				+ 2
				+ millisecondsWidth
				+ 2
				+ microsecondsWidth
				;

			lineAction(
				string.Format(
					"{0}{1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}  {9}  {10}  {11}",
					"",
					"Label".PadRight(totalMaxLabelLength + totalMaxLevel * indentWidth),
					"ms".PadLeft(millisecondsWidth, ' '),
					"µs".PadLeft(microsecondsWidth, ' '),
					"%".PadLeft(percentageWidth, ' '),
					"hits".PadLeft(hitCountWidth, ' '),
					"Avg ms".PadLeft(millisecondsWidth, ' '),
					"Avg µs".PadLeft(microsecondsWidth, ' '),
					"Min ms".PadLeft(millisecondsWidth, ' '),
					"Min µs".PadLeft(microsecondsWidth, ' '),
					"Max ms".PadLeft(millisecondsWidth, ' '),
					"Max µs".PadLeft(microsecondsWidth, ' ')
				)
			);
			lineAction("".PadRight(outputWidth, '='));

			#endregion

			#region Write body

			level = 0;
			Stack<bool> indentStack = new Stack<bool>();

			double stopWatchTicksPerMillisecond = Stopwatch.Frequency / 1000000.0;

			Traverse(
				(node, index, count) =>
				{
					indentStack.Push(index == count - 1);

					double percentage = (double)node.Total.DurationTicks / rootNode.Total.DurationTicks;

					lineAction(
						string.Format(
							"{0}{1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}  {9}  {10}  {11}",
							MakeIndentation(level, indentStack, indentChars[indentStyle]),
							node.Label.PadRight(totalMaxLabelLength + (totalMaxLevel - level) * indentWidth),
							node.Total.Duration.TotalMilliseconds.ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(millisecondsWidth, ' '),
							(node.Total.DurationTicks / stopWatchTicksPerMillisecond).ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(microsecondsWidth, ' '),
							percentage.ToString("p1", NumberFormatInfo.InvariantInfo).PadLeft(percentageWidth, ' '),
							node.Samples.Length.ToString().PadLeft(hitCountWidth, ' '),
							node.Total.AverageDuration.TotalMilliseconds.ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(millisecondsWidth, ' '),
							(node.Total.AverageDurationTicks / stopWatchTicksPerMillisecond).ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(microsecondsWidth, ' '),
							node.Total.MinDuration.TotalMilliseconds.ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(millisecondsWidth, ' '),
							(node.Total.MinDurationTicks / stopWatchTicksPerMillisecond).ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(microsecondsWidth, ' '),
							node.Total.MaxDuration.TotalMilliseconds.ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(millisecondsWidth, ' '),
							(node.Total.MaxDurationTicks / stopWatchTicksPerMillisecond).ToString("f1", NumberFormatInfo.InvariantInfo).PadLeft(microsecondsWidth, ' ')
						)
					);
					level++;
				},
				(node, index, count) => { level--; indentStack.Pop(); }
			);

			lineAction(string.Empty);

			#endregion
		}

		private void SerializeBinary(BinaryWriter binaryWriter)
		{
			binaryWriter.Write(binaryFileV1Marker.ToByteArray());
			rootNode.Serialize(binaryWriter);
		}

		private static ResultData UnserializeBinary(BinaryReader binaryReader)
		{
			byte[] marker = new byte[16];

			binaryReader.Read(marker, 0, 16);
			Guid readGuid = new Guid(marker);
			if(readGuid != binaryFileV1Marker)
				throw new ResultDataBinaryFileFormatException();

			ResultNode rootNode = ResultNode.Unserialize(binaryReader);

			return new ResultData(rootNode);
		}

		private void Traverse(Action<ResultNode, int, int> preAction, Action<ResultNode, int, int> postAction)
		{
			TraverseCore(rootNode, 0, 1, preAction, postAction);
		}

		private void TraverseCore(ResultNode node, int index, int count, Action<ResultNode, int, int> preAction, Action<ResultNode, int, int> postAction)
		{
			preAction(node, index, count);

			for(int i = 0; i < node.Children.Length; i++)
				TraverseCore(node.Children[i], i, node.Children.Length, preAction, postAction);

			postAction(node, index, count);
		}

		private string MakeIndentation(int level, Stack<bool> indentStack, string[] indentStrings)
		{
			StringBuilder sb = new StringBuilder();

			bool[] bs = indentStack.ToArray();

			for(int i = 0; i < level; i++)
			{
				bool b = bs[level - 1 - i];
				if(i == level - 1)
					if(b)
						sb.Append(indentStrings[0]);
					else
						sb.Append(indentStrings[1]);
				else
					if(b)
						sb.Append(indentStrings[2]);
					else
						sb.Append(indentStrings[3]);
			}

			return sb.ToString();
		}

		private string FormatDateTimeForJson(DateTime dateTime)
		{
			return string.Format(
				@"{0}-{1}-{2} {3}:{4}:{5}.{6}",
				dateTime.Year.ToString(NumberFormatInfo.InvariantInfo),
				dateTime.Month.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				dateTime.Day.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				dateTime.Hour.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				dateTime.Minute.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				dateTime.Second.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				dateTime.Millisecond.ToString(NumberFormatInfo.InvariantInfo).PadLeft(3, '0')
			);
		}

		private string FormatTimeSpanForJson(TimeSpan timeSpan)
		{
			return string.Format(
				@"{0}:{1}:{2}.{3}",
				timeSpan.Hours.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				timeSpan.Minutes.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				timeSpan.Seconds.ToString(NumberFormatInfo.InvariantInfo).PadLeft(2, '0'),
				timeSpan.Milliseconds.ToString(NumberFormatInfo.InvariantInfo).PadLeft(3, '0')
			);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Root node of the result profiling data.
		/// </summary>
		public ResultNode RootNode
		{
			get { return rootNode; }
		}

		#endregion

	}

}
