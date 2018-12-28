using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TriggersTools.ILPatching.Internal;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// The quantification for a regex opcheck.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public struct ILQuantifier : IFormattable {

		#region Constants

		/// <summary>
		/// The value of <see cref="int.MaxValue"/> which signifies "Or More" for use with * and +'s
		/// <see cref="Max"/> value.
		/// </summary>
		public const int OrMore = int.MaxValue;

		/// <summary>
		/// The quantifier signified by {1}.
		/// </summary>
		public static readonly ILQuantifier ExactlyOne = new ILQuantifier(1);
		/// <summary>
		/// The quantifier signified by ??, the same as {0,1}?.
		/// </summary>
		public static readonly ILQuantifier ZeroOrOne = new ILQuantifier(0, 1, false);
		/// <summary>
		/// The quantifier signified by *?, the same as {0,}?.
		/// </summary>
		public static readonly ILQuantifier ZeroOrMore = new ILQuantifier(0, OrMore, false);
		/// <summary>
		/// The quantifier signified by +?, the same as {1,}?.
		/// </summary>
		public static readonly ILQuantifier OneOrMore = new ILQuantifier(1, OrMore, false);

		/// <summary>
		/// The regex pattern used for parsing a quantifier.
		/// </summary>
		private const string ParsePattern = @"^(?:(?'token'[?*+])|\{(?'min'\d+)(?:(?'comma',)(?'max'\d+)?)?\})(?'lazy'\?)?$";
		/// <summary>
		/// The regex used for parsing a quantifier.
		/// </summary>
		private static readonly Regex ParseRegex = new Regex(ParsePattern);

		#endregion

		#region Fields

		/// <summary>
		/// Gets the minimum number of times the match must be complete.
		/// </summary>
		public int Min { get; }
		/// <summary>
		/// Gets the maximum number of times the match can be complete.
		/// </summary>
		public int Max { get; }
		/// <summary>
		/// Gets if the quantifier should try to get as many matches as possible.
		/// </summary>
		public bool IsGreedy { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets if the quantifier is not in use and has a <see cref="Min"/> and <see cref="Max"/> of 1.
		/// </summary>
		public bool IsOne => Min == 1 && Max == 1;
		/// <summary>
		/// Gets if the quantifier represents the same value for <see cref="Min"/> and <see cref="Max"/>.
		/// </summary>
		public bool IsExactly => Min == Max;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the quantifier to require a minimum and maximum amount.
		/// </summary>
		/// <param name="min">The minimum allowed amount.</param>
		/// <param name="max">The maximum allowed amount.</param>
		/// <param name="greedy">The quantifier should try to get as many matches as possible</param>
		/// 
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="min"/> is less than zero or <paramref name="max"/> is less than
		/// <paramref name="min"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Both <paramref name="min"/> and <paramref name="max"/> are zero.
		/// </exception>
		public ILQuantifier(int min, int max, bool greedy) {
			if (min < 0)
				throw new ArgumentOutOfRangeException(nameof(min), "Quantifier min is less than zero!");
			if (max < min)
				throw new ArgumentOutOfRangeException(nameof(max), "Quantifier max is less than min!");
			if (min == 0 && max == 0)
				throw new ArgumentException("Quantifier cannot be exactly zero!");
			Min = min;
			Max = max;
			IsGreedy = greedy;
		}
		/// <summary>
		/// Constructs the quantifier to require exactly a certain amount.
		/// </summary>
		/// <param name="exactly">The amount to require.</param>
		/// 
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="exactly"/> is less than zero.
		/// </exception>
		public ILQuantifier(int exactly) : this(exactly, exactly, true) { }

		#endregion

		#region Parsing

		/// <summary>
		/// Parses the quantifier just like a regex quantifier.
		/// </summary>
		/// <param name="s">The string to parse.</param>
		/// <returns>The parsed quantifier.</returns>
		/// 
		/// <remarks>
		/// Examples:
		/// Zero or one:  ?
		/// Zero or more: *
		/// One or more:  +
		/// 3 exactly:    {3}
		/// 3 or more:    {3,}
		/// 3 to 6:       {3,6}
		/// 
		/// Ending the quantifier with ? makes it ungreedy.
		/// </remarks>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="s"/> is null.
		/// </exception>
		/// <exception cref="FormatException">
		/// Bad quantifier format.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Min range is less than zero or max range is less than min range.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Both min range and max range are zero.
		/// </exception>
		public static ILQuantifier Parse(string s) {
			if (s == null)
				throw new ArgumentNullException(nameof(s));
			Match match = ParseRegex.Match(s);
			if (match.Success) {
				bool greedy = !match.Groups["lazy"].Success;
				Group group = match.Groups["token"];
				switch (group.Value) {
				case "?": return new ILQuantifier(0, 1, greedy);
				case "*": return new ILQuantifier(0, OrMore, greedy);
				case "+": return new ILQuantifier(1, OrMore, greedy);
				default:
					group = match.Groups["min"];
					bool comma = match.Groups["comma"].Success;
					int min = int.Parse(group.Value);
					if (comma) {
						int max = OrMore;
						group = match.Groups["max"];
						if (group.Success) {
							max = int.Parse(group.Value);
						}
						return new ILQuantifier(min, max, greedy);
					}
					else {
						// Exacly
						return new ILQuantifier(min);
					}
				}
			}
			throw new FormatException("Bad quantifier format!");
		}
		/// <summary>
		/// Parses the quantifier just like a regex quantifier.
		/// </summary>
		/// <param name="s">The string to parse.</param>
		/// <returns>The parsed quantifier.</returns>
		/// 
		/// <remarks>
		/// Examples:
		/// Zero or one:  ?
		/// Zero or more: *
		/// One or more:  +
		/// 3 exactly:    {3}
		/// 3 or more:    {3,}
		/// 3 to 6:       {3,6}
		/// 
		/// Ending the quantifier with ? makes it ungreedy.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Invalid format.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Negative minimum range or maximum is less than minimum.
		/// </exception>
		public static bool TryParse(string s, out ILQuantifier result) {
			try {
				result = Parse(s);
				return true;
			} catch {
				result = default;
				return false;
			}
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Returns the string representation of the quantifier with format "M".
		/// </summary>
		/// <returns>The string representation of the quantifier.</returns>
		public override string ToString() {
			return ToString("M", CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Returns the string representation of the quantifier with the specified format.<para/>
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// <returns>The string representation of the quantifier.</returns>
		/// 
		/// <remarks>
		/// Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public string ToString(string format) {
			return ToString(format, CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Returns the string representation of the quantifier with the specified format.<para/>
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// <param name="formatProvider">Unused.</param>
		/// <returns>The string representation of the quantifier.</returns>
		/// 
		/// <remarks>
		/// Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public string ToString(string format, IFormatProvider formatProvider) {
			string originalFormat = format;
			string greedy = (!IsGreedy ? "?" : "");
			bool bracesOnly = FormatUtils.HasToken("Bo", ref format);
			bool mandatory = FormatUtils.HasToken("M", ref format);

			if (format.Length != 0)
				throw new FormatException($"Invalid ILQuantifier format \"{originalFormat}\"!");

			if (!mandatory && IsOne)
				return string.Empty;
			if (!bracesOnly) {
				if (Min == 0) {
					if (Max == 1)
						return $"?{greedy}";
					else if (Max == OrMore)
						return $"*{greedy}";
				}
				else if (Min == 1 && Max == OrMore) {
					return $"+{greedy}";
				}
			}
			if (Min == Max)
				return $"{{{Min}}}";
			else if (Max == OrMore)
				return $"{{{Min},}}{greedy}";
			else
				return $"{{{Min},{Max}}}{greedy}";
		}
		private string DebuggerDisplay => ToString();
		/// <summary>
		/// Returns true if the two objects are quantifiers and equal to each other.
		/// </summary>
		/// <param name="obj">The objec to compare.</param>
		/// <returns>True if both objects are equal.</returns>
		public override bool Equals(object obj) {
			if (obj is ILQuantifier q)
				return this == q;
			return base.Equals(obj);
		}
		/// <summary>
		/// Returns true if the quantifier exactly matches the count.
		/// </summary>
		/// <param name="count">The count to match exactly.</param>
		/// <returns>True if the quantifier matches the count.</returns>
		public bool Equals(int count) {
			return Min == count && Max == count;
		}
		/// <summary>
		/// Returns true if the quantifier exactly matches the count.
		/// </summary>
		/// <param name="min">The minimum limit to match.</param>
		/// <param name="max">The maximum limit to match.</param>
		/// <returns>True if the quantifier matches the min and max.</returns>
		public bool Equals(int min, int max) {
			return Min == min && Max == max;
		}
		/// <summary>
		/// Gets the hash code of the quantifier.
		/// </summary>
		/// <returns>The hash code of the quantifier.</returns>
		public override int GetHashCode() => Max | (Min << 16) | (IsGreedy ? int.MinValue : 0);

		#endregion

		#region Operators

		public static bool operator ==(ILQuantifier a, ILQuantifier b) {
			return a.Min == b.Min && a.Max == b.Max && (a.IsGreedy == b.IsGreedy || a.IsExactly);
		}
		public static bool operator !=(ILQuantifier a, ILQuantifier b) {
			return a.Min != b.Min || a.Max != b.Max || (a.IsGreedy != b.IsGreedy && !a.IsExactly);
		}

		#endregion
	}
}
