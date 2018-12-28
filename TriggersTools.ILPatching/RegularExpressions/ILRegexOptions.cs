using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// Options to modify how <see cref="ILRegex"/> will behave when matching.
	/// </summary>
	[Flags]
	public enum ILRegexOptions {
		/// <summary>
		/// No options are specified.
		/// </summary>
		None = 0,
		/// <summary>
		/// Start (^) and End ($) checks will match to the boundaries of the search start and end inputs.
		/// </summary>
		SearchBoundaries = (1 << 0),
		/// <summary>
		/// Greedy quantifiers will be treated as lazy and vice-versa.
		/// </summary>
		Greedy = (1 << 1),
/*#if DEBUG
		/// <summary>
		/// Return a the closest match to success when the regex fails to find a full match.
		/// </summary>
		ClosestUnsuccessfulMatch = (1 << 2),
#endif*/
	}
}
