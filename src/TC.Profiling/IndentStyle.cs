using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TC.Profiling
{

	/// <summary>
	/// Indentation styles for <see cref="TC.Profiling.ResultData.ToFileAsText"/> and similar methods.
	/// </summary>
	public enum IndentStyle
	{
		/// <summary>
		/// Use unicode line drawing characters
		/// </summary>
		Unicode,

		/// <summary>
		/// Use ASCII characters
		/// </summary>
		Ascii
	}

}
