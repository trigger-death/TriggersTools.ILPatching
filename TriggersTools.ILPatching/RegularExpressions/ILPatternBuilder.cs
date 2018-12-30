using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A builder class for printing and appending <see cref="ILRegex"/> instruction checks.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public partial class ILPatternBuilder : IList<ILCheck>, IReadOnlyList<ILCheck> {
		#region Fields

		/// <summary>
		/// The expandable list of checks.
		/// </summary>
		private readonly List<ILCheck> checks;
		/// <summary>
		/// Gets or sets if quantifiers should be attached automatically.
		/// </summary>
		public bool AutoAttachQuantifiers { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the default <see cref="ILPattern"/> builder.
		/// </summary>
		public ILPatternBuilder() {
			checks = new List<ILCheck>();
		}
		/// <summary>
		/// Constructs the default <see cref="ILPattern"/> builder with the option of attaching quantifiers
		/// on <see cref="Add"/>.
		/// </summary>
		/// <param name="autoAttachQuantifiers">True if quantifiers should be attached automatically.</param>
		public ILPatternBuilder(bool autoAttachQuantifiers) {
			checks = new List<ILCheck>();
			AutoAttachQuantifiers = autoAttachQuantifiers;
		}
		/// <summary>
		/// Constructs the <see cref="ILPattern"/> builder from existing checks.
		/// </summary>
		/// <param name="checks">The checks to add to the builder.</param>
		public ILPatternBuilder(IEnumerable<ILCheck> checks) {
			this.checks = new List<ILCheck>(checks);
		}
		/// <summary>
		/// Constructs the <see cref="ILPattern"/> builder from existing checks with the option of attaching
		/// quantifiers on <see cref="Add"/>.
		/// </summary>
		/// <param name="autoAttachQuantifiers">True if quantifiers should be attached automatically.</param>
		/// <param name="checks">The checks to add to the builder.</param>
		public ILPatternBuilder(bool autoAttachQuantifiers, IEnumerable<ILCheck> checks) {
			this.checks = new List<ILCheck>(checks);
			AutoAttachQuantifiers = autoAttachQuantifiers;
		}
		/// <summary>
		/// Constructs the <see cref="ILPattern"/> builder from parsed pattern checks.
		/// </summary>
		/// <param name="patternStr">The string representation of the checks to add.</param>
		public ILPatternBuilder(string patternStr) {
			checks = new List<ILCheck>(ILCheck.ParseMany(patternStr));
		}
		/// <summary>
		/// Constructs the <see cref="ILPattern"/> builder from parsed pattern checks with the option of
		/// attaching quantifiers on <see cref="Add"/>.
		/// </summary>
		/// <param name="autoAttachQuantifiers">True if quantifiers should be attached automatically.</param>
		/// <param name="patternStr">The string representation of the checks to add.</param>
		public ILPatternBuilder(bool autoAttachQuantifiers, string patternStr) {
			checks = new List<ILCheck>(ILCheck.ParseMany(patternStr));
			AutoAttachQuantifiers = autoAttachQuantifiers;
		}

		#endregion

		#region AttachQuantifiers

		/// <summary>
		/// Attaches all standalone quantifier checks if they are right after a quantifiable check.
		/// </summary>
		/// <param name="throwOnDanglingQuantifier">
		/// True if an <see cref="ILRegexException"/> should be thrown when a quantifier is improperly placed.
		/// </param>
		/// 
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed and <paramref name="throwOnDanglingQuantifier"/> is true.
		/// </exception>
		public void AttachQuantifiers(bool throwOnDanglingQuantifier) {
			//bool lastCheckQuantified = false;
			ILCheck lastCheck = null;
			int lastCheckIndex = -1;
			for (int i = 0; i < checks.Count; i++) {
				//ILCheck lastCheck = (i != 0 ? checks[i - 1] : null);
				ILCheck check = checks[i];
				if (check.Code == OpChecks.Quantifier && (throwOnDanglingQuantifier ||
					(lastCheck != null && lastCheck.Quantifier.IsOne && /*!lastCheckQuantified &&*/
					lastCheck.Code != OpChecks.Quantifier && lastCheck.Code != OpChecks.GroupStart &&
					lastCheck.Code != OpChecks.Alternative)))
				{
					if (throwOnDanglingQuantifier) {
						if (lastCheck == null)
							throw new ILRegexException($"Unexpected quantifier check {check.Quantifier} at beginning of pattern!");
						else if (lastCheck.Code == OpChecks.GroupStart)
							throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to group start {lastCheck}!");
						else if (lastCheck.Code == OpChecks.Alternative)
							throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to altervative {lastCheck}!");
						else if (!lastCheck.Quantifier.IsOne)
							throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to an already quantified check {lastCheck}!");
					}
					checks[lastCheckIndex] = lastCheck = lastCheck.Repeat(check.Quantifier); // This clones the (sort of) immutable ILCheck
					//lastCheckQuantified = true;
					checks.RemoveAt(i--);
					continue;
				}
				lastCheck = check;
				lastCheckIndex = i;
				//lastCheckQuantified = false;
			}
		}

		#endregion

		#region Add (Collection Initializers)

		/// <summary>
		/// Adds a list of checks to the pattern with a conditional statement. Checks are only added when
		/// <paramref name="condition"/> is true.<para/>
		/// This method is used for collection initializers.
		/// </summary>
		/// <param name="condition">The condition to decided whether the checks are added.</param>
		/// <param name="checks">The checks to add if the conditional is true.</param>
		public void Add(bool condition, params ILCheck[] checks) {
			if (condition)
				Add(checks);
		}
		/// <summary>
		/// Add the range of checks to the pattern.<para/>
		/// This method is used for collection initializers.
		/// </summary>
		/// <param name="checks">The checks to add.</param>
		public void Add(IEnumerable<ILCheck> checks) {
			if (AutoAttachQuantifiers) {
				foreach (ILCheck check in checks)
					Add(check);
			}
			else {
				this.checks.AddRange(checks);
			}
		}
		/// <summary>
		/// Adds and parses the pattern checks.<para/>
		/// This method is used for collection initializers.
		/// </summary>
		/// <param name="patternStr">The string representation of the checks to add.</param>
		public void Add(string patternStr) {
			Add(ILCheck.ParseMany(patternStr));
		}

		#endregion

		#region IList Implementation

		/// <summary>
		/// Inserts the check at the specified index in the pattern.
		/// </summary>
		/// <param name="index">The index to insert the check at.</param>
		/// <param name="check">The check to insert.</param>
		public void Insert(int index, ILCheck check) {
			if (!TryAttachQuantifier(index, check))
				checks.Insert(index, check);
		}
		/// <summary>
		/// Adds the check to the pattern.
		/// </summary>
		/// <param name="check">The check to add.</param>
		public void Add(ILCheck check) => Insert(checks.Count, check);
		/// <summary>
		/// Clears the pattern of all checks.
		/// </summary>
		public void Clear() => checks.Clear();
		/// <summary>
		/// Test if the pattern contains the specified check.
		/// </summary>
		/// <param name="check">The check to look for.</param>
		/// <returns>True if the check was found.</returns>
		public bool Contains(ILCheck check) => checks.Contains(check);
		/// <summary>
		/// Gets the index of the specified check in the pattern.
		/// </summary>
		/// <param name="check">The check to look for.</param>
		/// <returns>The index if the check was found, otherwise -1.</returns>
		public int IndexOf(ILCheck check) => checks.IndexOf(check);
		/// <summary>
		/// Copies the checks in the pattern to the array.
		/// </summary>
		/// <param name="array">The array to copy the checks to.</param>
		/// <param name="arrayIndex">The index to start copying at.</param>
		public void CopyTo(ILCheck[] array, int arrayIndex) => checks.CopyTo(array, arrayIndex);
		/// <summary>
		/// Removes the spefified check from the pattern if it is found.
		/// </summary>
		/// <param name="check">The check to remove.</param>
		/// <returns>True if the check was found and removed.</returns>
		public bool Remove(ILCheck check) => checks.Remove(check);
		/// <summary>
		/// Removes the check at the spefified index from the pattern.
		/// </summary>
		/// <param name="index">The index of the check to remove.</param>
		public void RemoveAt(int index) => checks.RemoveAt(index);

		/// <summary>
		/// Gets or sets the check at the specified index in the pattern.
		/// </summary>
		/// <param name="index">The index of the check.</param>
		/// <returns>The check at the specified index.</returns>
		public ILCheck this[int index] {
			get => checks[index];
			set => checks[index] = value;
		}

		/// <summary>
		/// Gets the number of checks in the pattern.
		/// </summary>
		public int Count => checks.Count;

		/// <summary>
		/// Gets the enumerator for the pattern checks.
		/// </summary>
		/// <returns>The pattern's enumerator.</returns>
		public IEnumerator<ILCheck> GetEnumerator() => checks.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		bool ICollection<ILCheck>.IsReadOnly => false;

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
		public static ILPatternBuilder Parse(string s) {
			return new ILPatternBuilder(ILCheck.ParseMany(s));
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
		public static ILPatternBuilder FromFile(string filePath) {
			return Parse(File.ReadAllText(filePath));
		}

		#endregion

		#region Casting

		/// <summary>
		/// Constructs an immutable pattern from the pattern builder.
		/// </summary>
		/// <returns>The immutable pattern.</returns>
		public ILPattern ToPattern() => new ILPattern(this);
		public static implicit operator ILPattern(ILPatternBuilder builder) => new ILPattern(builder);

		#endregion

		#region TryAttachQuantifier

		/// <summary>
		/// Tries to attach the check to the previous if it is a quantifier.
		/// </summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="check">The check that may be a quantifier.</param>
		/// <returns>True if the quantifier was attached, and does not need to be added.</returns>
		private bool TryAttachQuantifier(int index, ILCheck check) {
			if (AutoAttachQuantifiers && index > 0 && check is ILCheck opCheck &&
				opCheck.Code == OpChecks.Quantifier) {
				if (index < 0 || index > checks.Count)
					throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the List.");
				if (checks[index - 1] is ILCheck lastOpCheck && !lastOpCheck.Quantifier.IsOne) {
					checks[index - 1] = lastOpCheck.Repeat(opCheck.Quantifier);
					return true;
				}
			}
			return false;
		}

		#endregion
	}
}
