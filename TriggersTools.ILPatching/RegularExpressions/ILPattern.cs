using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using TriggersTools.ILPatching.Internal;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// An immutable <see cref="ILRegex"/> pattern.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public partial class ILPattern : IReadOnlyList<ILCheck>, IFormattable {
		#region Constants
		
		/// <summary>
		/// The default format applied with <see cref="ToString"/>.
		/// </summary>
		internal const string DefaultToStringFormat = "";
		/// <summary>
		/// The default format applied with <see cref="Print"/>.
		/// </summary>
		internal const string DefaultPrintFormat = "I2";
		/// <summary>
		/// Just used to enforce ILCheck format when no checks are present.
		/// </summary>
		private static readonly ILCheck FormatCheck = new ILCheck(OpChecks.Nop) { Quantifier = ILQuantifier.ZeroOrOne };

		#endregion

		#region Fields

		/// <summary>
		/// The immutable list of checks in the pattern.
		/// </summary>
		internal ILCheck[] Checks { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an empty pattern.
		/// </summary>
		public ILPattern() : this(Enumerable.Empty<ILCheck>()) { }
		/// <summary>
		/// Constructs a pattern with the specified checks.
		/// </summary>
		/// <param name="checks">The checks to build the pattern from.</param>
		public ILPattern(IEnumerable<ILCheck> checks) {
			Checks = PrepareChecks(checks).ToArray();
		}

		private static IEnumerable<ILCheck> PrepareChecks(IEnumerable<ILCheck> checks) {
			ILCheck lastCheck = null;
			foreach (ILCheck check in checks) {
				if (check.Code == OpChecks.Quantifier && lastCheck != null && lastCheck.Quantifier.IsOne &&
					lastCheck.Code != OpChecks.Quantifier && lastCheck.Code != OpChecks.GroupStart &&
					lastCheck.Code != OpChecks.Alternative)
				{
					lastCheck = lastCheck.Repeat(check.Quantifier);
					continue;
				}
				if (lastCheck != null)
					yield return lastCheck;
				lastCheck = check;
			}
			if (lastCheck != null)
				yield return lastCheck;
		}

		#endregion

		#region Parsing

		/// <summary>
		/// Parses the string into an <see cref="ILRegex"/> pattern.
		/// </summary>
		/// <param name="s">The string representation of the pattern to parse.</param>
		/// <returns>The parsed pattern.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="s"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// A check's capture name is not a valid regex capture name.
		/// </exception>
		/// <exception cref="FormatException">
		/// A check was improperly formatted. Or an unexpected token was encountered.
		/// </exception>
		public static ILPattern Parse(string s) {
			return new ILPattern(ILCheck.ParseMany(s));
		}
		/// <summary>
		/// Parses the text from a file into an <see cref="ILRegex"/> pattern.
		/// </summary>
		/// <param name="filePath">The file path to get the text from.</param>
		/// <returns>The parsed pattern.</returns>
		/// 
		/// <exception cref="ArgumentException">
		/// <paramref name="filePath"/> is a zero-length string, contains only white space, or contains one
		/// or more invalid characters as defined by <see cref="Path.InvalidPathChars"/>. Or A check's
		/// capture name is not a valid regex capture name.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="filePath"/> is null.
		/// </exception>
		/// <exception cref="PathTooLongException">
		/// The specified path, file name, or both exceed the system-defined maximum length. For example, on
		/// Windows-based platforms, paths must be less than 248 characters, and file names must be less than
		/// 260 characters.
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		/// The specified path is invalid (for example, it is on an unmapped drive).
		/// </exception>
		/// <exception cref="IOException">
		/// An I/O error occurred while opening the file.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		/// <paramref name="filePath"/> specified a file that is read-only.-or- This operation is not
		/// supported on the current platform.-or- path specified a directory.-or- The caller does not have
		/// the required permission.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// The file specified in path was not found.
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// <paramref name="filePath"/> is in an invalid format.
		/// </exception>
		/// <exception cref="SecurityException">
		/// The caller does not have the required permission.
		/// </exception>
		/// <exception cref="FormatException">
		/// A check was improperly formatted. Or an unexpected token was encountered.
		/// </exception>
		public static ILPattern FromFile(string filePath) {
			return Parse(File.ReadAllText(filePath));
		}

		#endregion

		#region IReadOnlyList Implementation

		/// <summary>
		/// Gets the number of checks in the pattern.
		/// </summary>
		public int Count => Checks.Length;
		/// <summary>
		/// Gets the check at the specified index in the pattern.
		/// </summary>
		/// <param name="index">The index of the check.</param>
		/// <returns>The check at the specified index.</returns>
		public ILCheck this[int index] => Checks[index];

		/// <summary>
		/// Gets the enumerator for the pattern checks.
		/// </summary>
		/// <returns>The pattern's enumerator.</returns>
		public IEnumerator<ILCheck> GetEnumerator() => Checks.Cast<ILCheck>().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion
	}
}
