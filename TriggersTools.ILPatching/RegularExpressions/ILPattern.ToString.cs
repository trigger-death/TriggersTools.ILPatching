using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TriggersTools.ILPatching.Internal;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILPattern {
		#region ToString

		/// <summary>
		/// Gets the string representation of the pattern with no format, thus no indentation.
		/// </summary>
		/// <returns>The string representation of the pattern.</returns>
		public override string ToString() {
			return ToString(DefaultToStringFormat, CultureInfo.CurrentCulture);
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
			return ToString(format, CultureInfo.CurrentCulture);
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
			return ToString(Checks, format, formatProvider);
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
			Print(DefaultPrintFormat, CultureInfo.CurrentCulture);
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
			Print(format, CultureInfo.CurrentCulture);
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
			Print(Checks, format, formatProvider);
		}

		#endregion

		#region Static ToString

		/// <summary>
		/// Gets the string representation of the checks with the specified format.
		/// </summary>
		/// <param name="checks">The checks to output to a string.</param>
		/// <param name="format">The string format to use.</param>
		/// <returns>The string representation of the checks.</returns>
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
		public static string ToString(IReadOnlyList<ILCheck> checks, string format) {
			return ToString(checks, format, CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Gets the string representation of the checks with the specified format.
		/// </summary>
		/// <param name="checks">The checks to output to a string.</param>
		/// <param name="format">The string format to use.</param>
		/// <param name="formatProvider">Unused.</param>
		/// <returns>The string representation of the checks.</returns>
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
		public static string ToString(IReadOnlyList<ILCheck> checks, string format, IFormatProvider formatProvider) {
			string originalFormat = format;
			int indent = -1;
			if (FormatUtils.HasPattern(@"I(?'indent'\d+)", ref format, out Match indentMatch)) {
				if (!int.TryParse(indentMatch.Groups["indent"].Value, out indent))
					throw new FormatException($"Invalid ILPattern indent format \"{indentMatch.Value}\"!");
				if (!format.Contains("I")) // Don't add I back if already present
					format = "I" + format; // Indent for ILCheck
			}
			// Test format validity
			// An error will be thrown if the remaining quantifier format is not valid.
			if (format.Length != 0 && !checks.Any()) {
				try {
					FormatCheck.ToString(format, formatProvider);
				} catch (FormatException) {
					throw new FormatException($"Invalid ILPattern format \"{originalFormat}\"!");
				}
			}

			StringBuilder str = new StringBuilder();
			int level = 0;
			for (int i = 0; i < checks.Count; i++) {
				ILCheck check = checks[i];
				if (check.Code == OpChecks.GroupEnd)
					level--;

				if (indent >= 0) {
					if (check.Code != OpChecks.GroupEnd || i == 0 ||
						checks[i - 1].Code != OpChecks.GroupStart)
					{
						if (i != 0)
							str.AppendLine();
						str.Append(' ', Math.Max(0, level * indent));
					}
				}
				str.Append(check.ToString(format, formatProvider));

				if (check.Code == OpChecks.GroupStart)
					level++;

			}
			return str.ToString();
		}

		#endregion

		#region Static Print
		
		public static void Print(IReadOnlyList<ILCheck> checks, string format) {
			Print(checks, format, CultureInfo.CurrentCulture);
		}
		/// <summary>
		/// Outputs the pattern to the console with the specified format and syntax highlighting.
		/// </summary>
		/// <param name="checks">The checks to output to the console.</param>
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
		public static void Print(IReadOnlyList<ILCheck> checks, string format, IFormatProvider formatProvider) {
			string originalFormat = format;
			int indent = -1;
			if (FormatUtils.HasPattern(@"I(?'indent'\d+)", ref format, out Match indentMatch)) {
				if (!int.TryParse(indentMatch.Groups["indent"].Value, out indent))
					throw new FormatException($"Invalid ILPattern indent format \"{indentMatch.Value}\"!");
				format = "I" + format; // Indent for ILCheck
			}
			// Test format validity
			// An error will be thrown if the remaining quantifier format is not valid.
			if (format.Length != 0 && !checks.Any()) {
				try {
					FormatCheck.ToString(format, formatProvider);
				} catch (FormatException) {
					throw new FormatException($"Invalid ILPattern format \"{originalFormat}\"!");
				}
			}

			StringBuilder str = new StringBuilder();
			int level = 0;
			for (int i = 0; i < checks.Count; i++) {
				ILCheck check = checks[i];
				if (check.Code == OpChecks.GroupEnd)
					level--;

				if (indent >= 0) {
					if (check.Code != OpChecks.GroupEnd || i == 0 ||
						checks[i - 1].Code != OpChecks.GroupStart)
					{
						if (i != 0)
							Console.WriteLine();
						Console.Write(new string(' ', Math.Max(0, level * indent)));
					}
				}
				check.Print(format, formatProvider);

				if (check.Code == OpChecks.GroupStart)
					level++;
			}
		}

		#endregion

		#region OperandToString
		
		/// <summary>
		/// Outputs the operand object to an ILPattern-parsable string.
		/// </summary>
		/// <param name="operand">The operand to use.</param>
		/// <returns>The ILPattern-parsable string representation of the operand.</returns>
		public static string OperandToString(object operand) {
			if (operand == null)
				return "null";

			switch (operand) {
			case int value:
				return $"{value}";
			case long value:
				return $"{value}L";
			case byte value:
				return $"{value}b";
			case sbyte value:
				return $"{value}sb";
			case float value:
				return $"{value}f";
			case double value:
				return $"{value}d";
			case string value:
				return $"\"{value}\"";

			case ParameterDefinition value:
				return $"arg:\"{value}\"";
			case VariableDefinition value:
				return $"loc:\"{value}\"";
			case FieldReference value:
				return $"fld:\"{value.FullName.Substring(value.FullName.IndexOf(' ') + 1)}\"";
			case MethodReference value:
				return $"mth:\"{value.FullName.Substring(value.FullName.IndexOf(' ') + 1)}\"";
			case TypeReference value:
				return $"typ:\"{value.FullName}\"";
			case CallSite value:
				return $"cal:\"{value.FullName}\"";

			case Instruction value:
				return $"ins:{value}";
			case Instruction[] value:
				return $"ina:[{string.Join(",", (object[]) value)}]";
			}
			return null;
		}

		#endregion

		#region DebuggerDisplay

		private string DebuggerDisplay => $"Checks = {Count}";

		#endregion
	}
}
