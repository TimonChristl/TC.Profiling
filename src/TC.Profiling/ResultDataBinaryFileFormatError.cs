using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	/// <summary>
	/// Thrown by <see cref="TC.Profiling.ResultData.FromFileAsBinary"/> and 
	/// <see cref="TC.Profiling.ResultData.FromStreamAsBinary"/> when the contents are not recognized as valid binary result data.
	/// </summary>
	[Serializable]
	public class ResultDataBinaryFileFormatException : Exception
	{
		/// <inheritdoc/>
		public ResultDataBinaryFileFormatException() { }

		/// <inheritdoc/>
		public ResultDataBinaryFileFormatException(string message) : base(message) { }

		/// <inheritdoc/>
		public ResultDataBinaryFileFormatException(string message, Exception inner) : base(message, inner) { }

#if !NET8_0_OR_GREATER
		/// <inheritdoc/>
		protected ResultDataBinaryFileFormatException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
#endif
	}

}
