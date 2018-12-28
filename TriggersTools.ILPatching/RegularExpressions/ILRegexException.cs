using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// An exception thrown when compiling <see cref="ILRegex"/>.
	/// </summary>
	public class ILRegexException : Exception {
		/// <summary>
		/// Constructs an empty IL regex exception.
		/// </summary>
		public ILRegexException() { }
		/// <summary>
		/// Constructs the IL regex exception with the specified message.
		/// </summary>
		/// <param name="message">The exception's message.</param>
		public ILRegexException(string message) : base(message) { }
		/// <summary>
		/// Constructs the IL regex exception with the specified message and inner exception.
		/// </summary>
		/// <param name="message">The exception's message.</param>
		/// <param name="innerException">The exception's inner exception.</param>
		public ILRegexException(string message, Exception innerException) : base(message, innerException) { }
	}
}
