using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILCheck {
		#region Constants

		private const string QuantifierPattern = @"^(?:(?'token'[?*+])|\{(?'min'\d+)(?:(?'comma',)(?'max'\d+)?)?\})(?'lazy'\?)?";
		private const string GroupStartPattern = @"^\((?:(?'nocapture'\?:)|\?'(?'name'\w+)')?";
		private const string OpCodePattern = @"^(?'opcode'[A-Za-z0-9_.]+)$";

		//^<(?'code'[A-Za-z]+)[[:space:]]+(?'args'("(?:[^"\\]|\\.)*"|\w+)(?:[[:space:]]("(?:[^"\\]|\\.)*"|\w+))*)[[:space:]]*>
		private const string Arg = @"(?:[^""']+|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')"; // ("(?:[^"\\]|\\.)*"|\w+)
		private const string ArgumentPattern = @"^<(?'prefix'[A-Za-z]+)(?'args'(?:[[:space:]]+" + Arg + ")*)[[:space:]]*>";
		private const string AngleBracePattern = @"^<" + Arg + @"*>";

		private const string NopPattern = @"^<nop>$";
		private const string PrefixPattern = @"(?'prefix'(?:arg|loc|fld|mth|typ|cal|ins|ina):)?";
		private const string StringPattern = @"""(?'string'(?:[^""\\]|\\.)*)""";
		private const string NumberPattern = @"(?:(?'number'-?(?:\d*\.\d+|\d+))(?'postfix'[lbfd]|sb)?)";
		private const string OperandPattern = "^" + StringPattern + "|" + NumberPattern + "$";

		private const string CapturePattern = @"^'(?'capture'(?:[^'\\]|\\.)*)'$";
		private const string ValidateCapturePattern = @"^[A-Za-z_]\w*$";

		private static readonly Regex QuantifierRegex = new Regex(QuantifierPattern);
		private static readonly Regex GroupStartRegex = new Regex(GroupStartPattern);
		private static readonly Regex OpCodeRegex = new Regex(OpCodePattern);
		private static readonly Regex ArgRegex = new Regex(Arg, RegexOptions.IgnoreCase);
		private static readonly Regex ArgumentRegex = new Regex(ArgumentPattern, RegexOptions.IgnoreCase);
		private static readonly Regex AngleBraceRegex = new Regex(AngleBracePattern, RegexOptions.IgnoreCase);
		private static readonly Regex NopRegex = new Regex(NopPattern, RegexOptions.IgnoreCase);
		private static readonly Regex StringRegex = new Regex(StringPattern);
		private static readonly Regex OperandRegex = new Regex(OperandPattern, RegexOptions.IgnoreCase);
		private static readonly Regex CaptureRegex = new Regex(CapturePattern);
		private static readonly Regex ValidateCaptureRegex = new Regex(ValidateCapturePattern);



		#endregion

		#region Parsing

		public ILCheck Parse(string s) {
			ILCheck[] checks = ParseMany(s);
			if (checks.Length != 1)
				throw new FormatException($"Failed to parse 1 ILCheck, found {checks.Length}!");
			return checks[0];
		}

		public static ILCheck[] ParseMany(string s) {
			List<ILCheck> checks = new List<ILCheck>();
			ILCheck lastCheck = null;
			bool lastCheckQuantified = false;
			Match match;
			for (int i = 0; i < s.Length; i++) {
				bool lastCheckQuantifiedNew = false;
				char ch = s[i];
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
					if (!match.Success)
						throw new FormatException($"Poorly group start \"{s.Substring(i)}\"!");
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
					if (!match.Success)
						throw new FormatException($"Poorly formatted quantifier \"{s.Substring(i)}\"!");
					ILQuantifier quantifier = ILQuantifier.Parse(match.Value);
					if (checks.Count != 0 && lastCheck.Quantifier.IsOne && !lastCheckQuantified)
						lastCheck.Quantifier = quantifier;
					else
						checks.Add(ILChecks.Repeat(quantifier));
					i += match.Length - 1;
					lastCheckQuantifiedNew = true;
					break;
				case '<':
					Match angleMatch = AngleBraceRegex.Match(s, i);
					if (!angleMatch.Success)
						throw new FormatException("Missing closing check '>'!");

					match = ArgumentRegex.Match(s, i);
					if (!match.Success)
						throw new FormatException($"Poorly formated check \"{angleMatch.Value}\"!");
					string capture;
					string prefix = match.Groups["prefix"].Value;
					OpChecks opCheck = ParsePrefix(prefix);
					string[] args = match.Groups["args"].Value.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
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
							checks.Add(ILChecks.CaptureOperand(opCode));
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
						//checks.Add(ILChecks.CallSite(opCode, ParseString(args[1])));
						break;
					}
					i += angleMatch.Length - 1;
					break;
				}
				lastCheckQuantified = lastCheckQuantifiedNew;
			}
			return checks.ToArray();
		}

		private string DequoteArgDouble(string arg) {
			if (arg.StartsWith("\"") && arg.EndsWith("\"") && arg.Length >= 2)
				return arg.Substring(1, arg.Length - 2);
			return arg;
		}
		private string DequoteArgSingle(string arg) {
			if (arg.StartsWith("\"") && arg.EndsWith("\"") && arg.Length >= 2)
				return arg.Substring(1, arg.Length - 2);
			return arg;
		}

		#endregion

		#region Operand Parsing


		public static object ParseOperand(string s) {
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

		#endregion

		#region Capture Parsing


		public static string ParseCapture(string s, bool allowIndex) {
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

		#endregion

		#region String Parsing

		public static string ParseString(string s) {
			Match match = StringRegex.Match(s);
			if (!match.Success)
				throw new FormatException($"Failed to parse string \"{s}\"!");
			return match.Groups["string"].Value;
		}

		#endregion
	}
}
