using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILRegex {
		#region ToString

		/// <summary>
		/// Gets the string representation of the pattern with no format, thus no indentation.
		/// </summary>
		/// <returns>The string representation of the pattern.</returns>
		public override string ToString() {
			return Pattern.ToString();
		}
		/// <summary>
		/// Gets the string representation of the pattern with the specified format.
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// <returns>The string representation of the pattern.</returns>
		/// 
		/// <remarks>
		/// ILPattern Format Flags:
		/// I# = Indented: Instead of aligning everything on one line, each line will be indented by group
		/// level using # spaces.
		/// <para/>
		/// ILCheck Format Flags:
		/// Nq = No quantifier: The quantifier is not added, even when not exactly one.
		/// U  = Upper: The prefix codes are output in uppercase.
		/// I  = Indented: Prefix codes are indented to all align. This is automatically present if I# is
		/// present.
		/// <para/>
		/// Quantifier Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="ILQuantifier.IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public string ToString(string format) {
			return Pattern.ToString(format, CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Gets the string representation of the pattern with the specified format.
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// <param name="formatProvider">Unused.</param>
		/// <returns>The string representation of the pattern.</returns>
		/// 
		/// <remarks>
		/// ILPattern Format Flags:
		/// I# = Indented: Instead of aligning everything on one line, each line will be indented by group
		/// level using # spaces.
		/// <para/>
		/// ILCheck Format Flags:
		/// Nq = No quantifier: The quantifier is not added, even when not exactly one.
		/// U  = Upper: The prefix codes are output in uppercase.
		/// I  = Indented: Prefix codes are indented to all align. This is automatically present if I# is
		/// present.
		/// <para/>
		/// Quantifier Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="ILQuantifier.IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public string ToString(string format, IFormatProvider formatProvider) {
			return Pattern.ToString(format, formatProvider);
		}

		#endregion

		#region Print

		/// <summary>
		/// Outputs the pattern to the console with the format "I2" and syntax highlighting.
		/// </summary>
		/// 
		/// <remarks>
		/// ILPattern Format Flags:
		/// I# = Indented: Instead of aligning everything on one line, each line will be indented by group
		/// level using # spaces.
		/// <para/>
		/// ILCheck Format Flags:
		/// Nq = No quantifier: The quantifier is not added, even when not exactly one.
		/// U  = Upper: The prefix codes are output in uppercase.
		/// I  = Indented: Prefix codes are indented to all align. This is automatically present if I# is
		/// present.
		/// <para/>
		/// Quantifier Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="ILQuantifier.IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public void Print() {
			Pattern.Print();
		}
		/// <summary>
		/// Outputs the pattern to the console with the specified format and syntax highlighting.
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// 
		/// <remarks>
		/// ILPattern Format Flags:
		/// I# = Indented: Instead of aligning everything on one line, each line will be indented by group
		/// level using # spaces.
		/// <para/>
		/// ILCheck Format Flags:
		/// Nq = No quantifier: The quantifier is not added, even when not exactly one.
		/// U  = Upper: The prefix codes are output in uppercase.
		/// I  = Indented: Prefix codes are indented to all align. This is automatically present if I# is
		/// present.
		/// <para/>
		/// Quantifier Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="ILQuantifier.IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public void Print(string format) {
			Pattern.Print(format, CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Outputs the pattern to the console with the specified format and syntax highlighting.
		/// </summary>
		/// <param name="format">The string format to use.</param>
		/// <param name="formatProvider">Unused.</param>
		/// 
		/// <remarks>
		/// ILPattern Format Flags:
		/// I# = Indented: Instead of aligning everything on one line, each line will be indented by group
		/// level using # spaces.
		/// <para/>
		/// ILCheck Format Flags:
		/// Nq = No quantifier: The quantifier is not added, even when not exactly one.
		/// U  = Upper: The prefix codes are output in uppercase.
		/// I  = Indented: Prefix codes are indented to all align. This is automatically present if I# is
		/// present.
		/// <para/>
		/// Quantifier Format Flags:
		/// Bo = Brace format only: Shorthand tokens ?/*/+ will not be used.
		/// M  = Mandatory: String will not be empty if <see cref="ILQuantifier.IsOne"/> is true.
		/// </remarks>
		/// 
		/// <exception cref="FormatException">
		/// Unknown flags were passed in <paramref name="format"/>.
		/// </exception>
		public void Print(string format, IFormatProvider formatProvider) {
			Pattern.Print(format, formatProvider);
		}

		#endregion

		#region DebuggerDisplay

		private string DebuggerDisplay => $"Checks = {Pattern.Count}, Options = {Options}";

		#endregion
	}
}
