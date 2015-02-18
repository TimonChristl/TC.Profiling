using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Profiling
{

	/// <summary>
	/// A single node of profiling result data. A node has samples (<see cref="Samples"/>),
	/// Statistical information about these samples (<see cref="Total"/>) and
	/// can have child nodes (<see cref="Children"/>).
	/// </summary>
	public class ResultNode
	{

		#region Private fields

		private int id;
		private string label;
		private ResultTotalSample total;
		private ResultSample[] samples;
		private ResultNode[] children;

		#endregion

		#region Constructors

		internal ResultNode()
		{
			this.id = 0;
			this.label = string.Empty;
			this.total = new ResultTotalSample();
			this.samples = new ResultSample[0];
			this.children = new ResultNode[0];
		}

		internal ResultNode(int id, string label, ResultTotalSample total, ResultSample[] samples, ResultNode[] children)
		{
			this.id = id;
			this.label = label;
			this.total = total;
			this.samples = samples;
			this.children = children;
		}

		#endregion

		#region Internal methods

		internal void Serialize(BinaryWriter binaryWriter)
		{
			binaryWriter.Write(id);
			binaryWriter.Write(label);

			ResultTotalSample.Serialize(binaryWriter, total);

			binaryWriter.Write(samples.Length);
			for(int i = 0; i < samples.Length; i++)
				ResultSample.Serialize(binaryWriter, samples[i]);

			binaryWriter.Write(children.Length);
			for(int i = 0; i < children.Length; i++)
				children[i].Serialize(binaryWriter);
		}

		internal static ResultNode Unserialize(BinaryReader binaryReader)
		{
			int id = binaryReader.ReadInt32();
			string label = binaryReader.ReadString();

			ResultTotalSample total = ResultTotalSample.Unserialize(binaryReader);

			int numSamples = binaryReader.ReadInt32();
			ResultSample[] samples = new ResultSample[numSamples];
			for(int i = 0; i < samples.Length; i++)
				samples[i] = ResultSample.Unserialize(binaryReader);

			int numChildren = binaryReader.ReadInt32();
			ResultNode[] children = new ResultNode[numChildren];
			for(int i = 0; i < children.Length; i++)
				children[i] = ResultNode.Unserialize(binaryReader);

			return new ResultNode(id, label, total, samples, children);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Id of the node.
		/// </summary>
		public int Id
		{
			get { return id; }
		}

		/// <summary>
		/// Label of the node.
		/// </summary>
		public string Label
		{
			get { return label; }
		}

		/// <summary>
		/// Child nodes of the node.
		/// </summary>
		public ResultNode[] Children
		{
			get { return children; }
		}

		/// <summary>
		/// Samples of the node.
		/// </summary>
		public ResultSample[] Samples
		{
			get { return samples; }
		}

		/// <summary>
		/// Statistical information about the node's samples.
		/// </summary>
		public ResultTotalSample Total
		{
			get { return total; }
		}

		#endregion

	}

}
