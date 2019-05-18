using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILCheck {
		#region Constants

		private const string QuantifierPattern = @"(?:(?'token'[?*+])|\{(?'min'\d+)(?:(?'comma',)(?'max'\d+)?)?\})(?'lazy'\?)?";
		private const string GroupStartPattern = @"\((?:(?'nocapture'\?:)|\?'(?'name'(?:[^'\\]|\\.)*)')?";

		private const string WS = @"(?:\s|/\*.*?(?:\*/|$)|//.*?(\r\n|\r|\n|$))";
		private const string Arg = @"(?:[^""'>\s]+?|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')";
		private const string ArgumentPattern = @"<(?'prefix'[a-z]+)(?'args'(?:" + WS + @"+" + Arg + @")*)" + WS + @"*>";
		private const string AngleBracePattern = @"<" + WS + @"*(?:" + Arg + WS + @"*)*>";
		private const string ArgSplitPattern = @"\s+|/\*.*?\*/|//.*?(?:\r\n|\r|\n)";
		
		private const string StringPattern = @"""(?'string'(?:[^""\\]|\\.)*)""";
		private const string NumberPattern = @"(?:(?'number'-?(?:\d*\.\d+|\d+))(?'postfix'[lbfd]|sb)?)";
		private const string OperandPattern = "^" + StringPattern + "|" + NumberPattern + "$";
		private const string CapturePattern = @"^'(?'capture'(?:[^'\\]|\\.)*)'$";

		private const string CommentStartPattern = @"(?'line'//)|(?'block'/\*)";
		private const string CommentEndPattern = @"\*/";
		
		private static readonly Regex QuantifierRegex = new Regex(QuantifierPattern, RegexOptions.Singleline);
		private static readonly Regex GroupStartRegex = new Regex(GroupStartPattern, RegexOptions.Singleline);
		private static readonly Regex ArgumentRegex = new Regex(ArgumentPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static readonly Regex AngleBraceRegex = new Regex(AngleBracePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static readonly Regex ArgSplitRegex = new Regex(ArgSplitPattern, RegexOptions.Singleline);
		private static readonly Regex StringRegex = new Regex(StringPattern, RegexOptions.Singleline);
		private static readonly Regex OperandRegex = new Regex(OperandPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private static readonly Regex CaptureRegex = new Regex(CapturePattern, RegexOptions.Singleline);
		private static readonly Regex CommentStartRegex = new Regex(CommentStartPattern, RegexOptions.Singleline);
		private static readonly Regex CommentEndRegex = new Regex(CommentEndPattern, RegexOptions.Singleline);

		#endregion

		#region Parsing

		/// <summary>
		/// Parses a single <see cref="ILRegex"/> check.
		/// </summary>
		/// <param name="s">The string representation of the single check to parse.</param>
		/// <returns>The parsed check.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="s"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// A check's capture name is not a valid regex capture name.
		/// </exception>
		/// <exception cref="FormatException">
		/// A check was improperly formatted. Or, there was more than one check. Or an unexpected token was
		/// encountered.
		/// </exception>
		public ILCheck Parse(string s) {
			ILCheck[] checks = ParseMany(s);
			if (checks.Length != 1)
				throw new FormatException($"Failed to parse 1 ILCheck, found {checks.Length}!");
			return checks[0];
		}
		/// <summary>
		/// Parses the string into <see cref="ILRegex"/> checks.
		/// </summary>
		/// <param name="s">The string representation of the checks to parse.</param>
		/// <returns>The parsed checks.</returns>
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
		public static ILCheck[] ParseMany(string s) {
			if (s == null)
				throw new ArgumentNullException(nameof(s));
			List<ILCheck> checks = new List<ILCheck>();
			ILCheck lastCheck = null;
			Match match;
			bool lineComment = false;
			bool blockComment = false;
			int i = 0;
			string AddLocation(Exception ex) {
				var matches = Regex.Matches(s.Substring(0, i), @"\r\n|\r|\n", RegexOptions.Singleline);
				int line = matches.Count + 1;
				int ch = i + 1;
				if (matches.Count != 0) {
					Match lastMatch = matches[matches.Count - 1];
					ch = i - (lastMatch.Index + lastMatch.Length) + 1;
				}
				string atLocation = $" at Line {line}, Ch {ch}";
				string message = ex.Message;
				string punctuation = "!";
				if (message.EndsWith("!")) {
					punctuation = "!";
					message = message.Substring(0, message.Length - 1);
				}
				else if (message.EndsWith(".")) {
					punctuation = ".";
					message = message.Substring(0, message.Length - 1);
				}
				return $"{message}{atLocation}{punctuation}";
			}
			try {
				for (i = 0; i < s.Length; i++) {
					char ch = s[i];
					
					if (blockComment) {
						match = CommentEndRegex.Match(s, i);
						if (match.Success && match.Index == i) {
							i++;
							blockComment = false;
						}
						continue;
					}
					else if (lineComment) {
						if (ch == '\n')
							lineComment = false;
						continue;
					}

					if (char.IsWhiteSpace(ch))
						continue;

					switch (ch) {
					case '.': checks.Add(ILChecks.Any); break;
					case '^': checks.Add(ILChecks.Start); break;
					case '$': checks.Add(ILChecks.End); break;
					case '|': checks.Add(ILChecks.Alternative); break;
					case ')': checks.Add(ILChecks.GroupEnd); break;
					case '(':
						match = GroupStartRegex.Match(s, i);
						if (!match.Success || match.Index != i)
							throw new FormatException($"Poorly formatted group start \"{s.Substring(i)}\"!");
						if (match.Groups["nocapture"].Success)
							checks.Add(ILChecks.GroupStart);
						else if (match.Groups["name"].Success)
							checks.Add(ILChecks.CaptureGroupStart(match.Groups["name"].Value));
						else
							checks.Add(ILChecks.CaptureGroupStart());
						i += match.Length - 1;
						break;
					case '?':
					case '*':
					case '+':
					case '{':
						match = QuantifierRegex.Match(s, i);
						if (!match.Success || match.Index != i)
							throw new FormatException($"Poorly formatted quantifier!");
						ILQuantifier quantifier = ILQuantifier.Parse(match.Value);
						if (checks.Count != 0 && lastCheck != null && lastCheck.Quantifier.IsOne)
							lastCheck.Quantifier = quantifier;
						else
							checks.Add(ILChecks.Repeat(quantifier));
						i += match.Length - 1;
						break;
					case '<':
						Match angleMatch = AngleBraceRegex.Match(s, i);
						if (!angleMatch.Success || angleMatch.Index != i)
							throw new FormatException("Missing closing check '>'!");

						match = ArgumentRegex.Match(s, i);
						if (!match.Success || match.Index != i)
							throw new FormatException($"Poorly formated check \"{angleMatch.Value}\"!");
						string capture;
						string prefix = match.Groups["prefix"].Value;
						OpChecks opCheck = ParsePrefix(prefix);
						string[] args = ArgSplitRegex.Split(match.Groups["args"].Value)
													 .Where(a => a != string.Empty)
													 .ToArray();
						if (args.Length == 0 || (opCheck != OpChecks.OpCode && opCheck != OpChecks.Operand && args.Length == 1))
							throw new FormatException($"Not enough arguments for {prefix} in check \"{angleMatch.Value}\"!");
						if (args.Length > 2)
							throw new FormatException($"Too many arguments for {prefix} in check \"{angleMatch.Value}\"!");
						AnyOpCode opCode = AnyOpCode.Parse(args[0]);
						switch (opCheck) {
						case OpChecks.OpCode:
							if (args.Length == 2)
								checks.Add(ILChecks.OpCode(opCode, ParseOperand(args[1])));
							else
								checks.Add(ILChecks.OpCode(opCode));
							break;
						case OpChecks.Operand:
							if (args.Length == 2)
								checks.Add(ILChecks.CaptureOperand(opCode, ParseCapture(args[1], false)));
							else
								checks.Add(ILChecks.CaptureOperand(opCode)); // Unnamed
							break;
						case OpChecks.OperandEquals:
							capture = ParseCapture(args[1], true);
							if (int.TryParse(capture, out int index))
								checks.Add(ILChecks.EqualsOperand(opCode, index));
							else
								checks.Add(ILChecks.EqualsOperand(opCode, capture));
							break;
						case OpChecks.FieldName:
							checks.Add(ILChecks.Field(opCode, ParseString(args[1])));
							break;
						case OpChecks.MethodName:
							checks.Add(ILChecks.Method(opCode, ParseString(args[1])));
							break;
						case OpChecks.TypeName:
							checks.Add(ILChecks.Type(opCode, ParseString(args[1])));
							break;
						case OpChecks.CallSiteName:
							checks.Add(ILChecks.CallSite(opCode, ParseString(args[1])));
							break;
						}
						i += angleMatch.Length - 1;
						break;
					case '/':
						match = CommentStartRegex.Match(s, i);
						if (!match.Success || match.Index != i) {
							goto default;
						}
						else if (match.Groups["line"].Success) {
							i++;
							lineComment = true;
						}
						else if (match.Groups["block"].Success) {
							i++;
							blockComment = true;
						}
						break;

					default:
						throw new FormatException($"Unexpected token '{ch}' while parsing IL Regex checks!");
					}
				}
			} catch (ArgumentOutOfRangeException ex) {
				throw new ArgumentOutOfRangeException(AddLocation(ex), ex);
			} catch (ArgumentException ex) {
				throw new ArgumentException(AddLocation(ex), ex);
			} catch (FormatException ex) {
				throw new FormatException(AddLocation(ex), ex);
			} catch (Exception ex) {
				throw new Exception(AddLocation(ex), ex);
			}
			return checks.ToArray();
		}

		#endregion

		#region Parsing Types

		/// <summary>
		/// Parses an ILPattern-complient operand.
		/// </summary>
		/// <param name="s">The string representation of the operand.</param>
		/// <returns>The operand value.</returns>
		internal static object ParseOperand(string s) {
			Match match = OperandRegex.Match(s);
			if (!match.Success)
				throw new FormatException($"Failed to parse operand \"{s}\"!");
			Group group = match.Groups["string"];
			if (group.Success) {
				return group.Value;
			}
			else {
				// Else number
				string number = match.Groups["number"].Value;
				string postfix = match.Groups["postfix"].Value;
				switch (postfix.ToLower()) {
				case "": return int.Parse(number);
				case "l": return long.Parse(number);
				case "b": return byte.Parse(number);
				case "sb": return sbyte.Parse(number);
				case "f": return float.Parse(number);
				case "d": return double.Parse(number);
				default: throw new FormatException($"Unexpected numeric postfix {postfix}!");
				}
			}
		}
		/// <summary>
		/// Parses an ILPattern-complient capture.
		/// </summary>
		/// <param name="s">The string representation of the capture.</param>
		/// <returns>The capture name.</returns>
		internal static string ParseCapture(string s, bool allowIndex) {
			Match match = CaptureRegex.Match(s);
			if (!match.Success)
				throw new FormatException($"Failed to parse capture \"{s}\"!");
			string capture = match.Groups["capture"].Value;
			if (capture.Length == 0)
				throw new FormatException($"Capture cannot be empty!");
			if (int.TryParse(capture, out int index) && !allowIndex)
				throw new FormatException($"Cannot use capture index \"{s}\" with operand capture!");
			return capture;
		}
		/// <summary>
		/// Parses a quoted string and dequotes it.
		/// </summary>
		/// <param name="s">The quoted string.</param>
		/// <returns>The string value.</returns>
		public static string ParseString(string s) {
			Match match = StringRegex.Match(s);
			if (!match.Success)
				throw new FormatException($"Failed to parse string \"{s}\"!");
			return match.Groups["string"].Value;
		}

		#endregion
	}
}
