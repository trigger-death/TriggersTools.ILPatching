using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A Regular Expression for matching CIL instructions.
	/// </summary>
	public partial class ILRegex {
		#region Static Debug Fields

		//[Conditional("DEBUG")]
#if DEBUG
		/// <summary>
		/// True if debug information should be output when running ILRegex.
		/// </summary>
		public static bool VerboseDebug { get; set; } = false;
		/// <summary>
		/// Only debug the top level checks.
		/// </summary>
		public static bool FirstLevelDebugOnly { get; set; } = true;
#endif

		#endregion

		#region Fields

		/// <summary>
		/// Gets the pattern used to compile this regex.
		/// </summary>
		public ILPattern Pattern { get; }
		/// <summary>
		/// Gets the matching options for this regex.
		/// </summary>
		public ILRegexOptions Options { get; }
		/// <summary>
		/// Gets the number of group captures.
		/// </summary>
		public int GroupCount { get; }
		/// <summary>
		/// Gets the number of operand captures.
		/// </summary>
		public int OperandCount { get; }
		/// <summary>
		/// Gets the compiled opchecks.
		/// </summary>
		internal ILCheck[] CompiledOpChecks { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the IL Regex from the pattern.
		/// </summary>
		/// <param name="pattern">The IL check pattern.</param>
		/// <param name="options">The matching options to use for the regex.</param>
		public ILRegex(ILPattern pattern) : this(pattern, ILRegexOptions.None) { }
		/// <summary>
		/// Constructs the IL Regex from the pattern.
		/// </summary>
		/// <param name="pattern">The IL check pattern.</param>
		/// <param name="options">The matching options to use for the regex.</param>
		public ILRegex(ILPattern pattern, ILRegexOptions options) {
			Pattern = pattern;
			Options = options;
			CompiledOpChecks = ILRegexCompiler.Compile(pattern.Checks, out int groupCount, out int operandCount);
			GroupCount = groupCount;
			OperandCount = operandCount;
		}

		#endregion

		#region Instance Match

		/// <summary>
		/// Matches the method's instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(MethodDefinition method, int start, int end) {
			return ILRegexRunner.Run(this, method, start, end);
		}
		/// <summary>
		/// Matches the method's instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(MethodDefinition method, int start) {
			return ILRegexRunner.Run(this, method, start, null);
		}
		/// <summary>
		/// Matches the method's instructions to the whole pattern.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(MethodDefinition method) {
			return ILRegexRunner.Run(this, method, null, null);
		}
		/// <summary>
		/// Matches the  instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(Instruction[] instructions, int start, int end) {
			return ILRegexRunner.Run(this, instructions, start, end);
		}
		/// <summary>
		/// Matches the instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(Instruction[] instructions, int start) {
			return ILRegexRunner.Run(this, instructions, start, null);
		}
		/// <summary>
		/// Matches the  instructions to the whole pattern.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public ILMatch Match(Instruction[] instructions) {
			return ILRegexRunner.Run(this, instructions, null, null);
		}

		#endregion

		#region Static Match

		/// <summary>
		/// Matches the method's instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(MethodDefinition method, int start, int end, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(method, start, end);
		}
		/// <summary>
		/// Matches the method's instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(MethodDefinition method, int start, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(method, start);
		}
		/// <summary>
		/// Matches the method's instructions to the whole pattern.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(MethodDefinition method, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(method);
		}
		/// <summary>
		/// Matches the  instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(Instruction[] instructions, int start, int end, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(instructions, start, end);
		}
		/// <summary>
		/// Matches the instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(Instruction[] instructions, int start, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(instructions, start);
		}
		/// <summary>
		/// Matches the  instructions to the whole pattern.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		public static ILMatch Match(Instruction[] instructions, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(instructions);
		}

		#endregion
	}
}
