using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TriggersTools.ILPatching.Internal;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// An immutable <see cref="ILRegex"/> pattern.
	/// </summary>
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
			bool lastCheckQuantified = false;
			foreach (ILCheck check in checks) {
				if (check.Code == OpChecks.Quantifier && lastCheck != null && lastCheck.Quantifier.IsOne &&
					lastCheck.Code != OpChecks.Quantifier && lastCheck.Code != OpChecks.GroupStart &&
					!lastCheckQuantified)
				{
					/*if (lastCheck == null)
						throw new ILRegexException($"Unexpected quantifier check {check.Quantifier} at beginning of pattern!");
					else if (!lastCheck.Quantifier.IsOne || lastCheckQuantified)
						throw new ILRegexException($"Cannot attach quantifier to an already-quantified check {lastCheck}!");*/
					lastCheck = lastCheck.Repeat(check.Quantifier); // This clones the (sort of) immutable ILCheck
					yield return lastCheck;
					lastCheckQuantified = true;
					continue;
				}
				if (lastCheck != null && !lastCheckQuantified)
					yield return lastCheck;
				lastCheck = check;
				lastCheckQuantified = false;
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
		public ILPattern Parse(string s) {
			return new ILPattern(ILCheck.ParseMany(s));
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
