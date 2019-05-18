using System;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// An exception thrown when the executable has already been patched.
	/// </summary>
	public class AlreadyPatchedException : Exception {
		/// <summary>
		/// Constructs the exception with no message or inner exception.
		/// </summary>
		public AlreadyPatchedException() : base() { }
		/// <summary>
		/// Constructs the exception with the specified message and no inner exception.
		/// </summary>
		/// <param name="message">The message for the exception.</param>
		public AlreadyPatchedException(string message) : base(message) { }
		/// <summary>
		/// Constructs the exception with the specified message and inner exception.
		/// </summary>
		/// <param name="message">The message for the exception.</param>
		/// <param name="innerException">The inner exception inside this exception.</param>
		public AlreadyPatchedException(string message, Exception innerException) : base(message, innerException) { }
	}
	/// <summary>
	/// An exception thrown when the patcher is unable to locate the instructions to change.
	/// </summary>
	public class PatcherException : Exception {
		/// <summary>
		/// Constructs the exception with no message or inner exception.
		/// </summary>
		public PatcherException() : base() { }
		/// <summary>
		/// Constructs the exception with the specified message and no inner exception.
		/// </summary>
		/// <param name="message">The message for the exception.</param>
		public PatcherException(string message) : base(message) { }
		/// <summary>
		/// Constructs the exception with the specified message and inner exception.
		/// </summary>
		/// <param name="message">The message for the exception.</param>
		/// <param name="innerException">The inner exception inside this exception.</param>
		public PatcherException(string message, Exception innerException) : base(message, innerException) { }
	}
}
