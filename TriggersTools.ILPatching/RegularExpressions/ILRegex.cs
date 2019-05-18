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
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public partial class ILRegex {
		#region Static Debug Fields
		
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public ILRegex(ILPattern pattern) : this(pattern, ILRegexOptions.None) { }
		/// <summary>
		/// Constructs the IL Regex from the pattern.
		/// </summary>
		/// <param name="pattern">The IL check pattern.</param>
		/// <param name="options">The matching options to use for the regex.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public ILRegex(ILPattern pattern, ILRegexOptions options) {
			Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method, int start, int end) {
			return ILRegexRunner.Run(this, method, null, start, end);
		}
		/// <summary>
		/// Matches the method's instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method, int start) {
			return ILRegexRunner.Run(this, method, null, start, null);
		}
		/// <summary>
		/// Matches the method's instructions to the whole pattern.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method) {
			return ILRegexRunner.Run(this, method, null, null, null);
		}
		/// <summary>
		/// Matches the  instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions, int start, int end) {
			return ILRegexRunner.Run(this, instructions, null, start, end);
		}
		/// <summary>
		/// Matches the instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions, int start) {
			return ILRegexRunner.Run(this, instructions, null, start, null);
		}
		/// <summary>
		/// Matches the  instructions to the whole pattern.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions) {
			return ILRegexRunner.Run(this, instructions, null, null, null);
		}

		#endregion

		#region Instance Match (OperandDictionary)

		/// <summary>
		/// Matches the method's instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary, int start,
			int end)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, method, operandDictionary, start, end);
		}
		/// <summary>
		/// Matches the method's instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary, int start) {
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, method, operandDictionary, start, null);
		}
		/// <summary>
		/// Matches the method's instructions to the whole pattern.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary) {
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, method, operandDictionary, null, null);
		}
		/// <summary>
		/// Matches the  instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary, int start,
			int end)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, instructions, operandDictionary, start, end);
		}
		/// <summary>
		/// Matches the instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary, int start) {
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, instructions, operandDictionary, start, null);
		}
		/// <summary>
		/// Matches the  instructions to the whole pattern.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="operandDictionary"/> is null.
		/// </exception>
		public ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary) {
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return ILRegexRunner.Run(this, instructions, operandDictionary, null, null);
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
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
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/> or <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(Instruction[] instructions, ILPattern pattern,
			ILRegexOptions options = default)
		{
			return new ILRegex(pattern, options).Match(instructions);
		}

		#endregion

		#region Static Match (OperandDictionary)

		/// <summary>
		/// Matches the method's instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary,
			int start, int end, ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(method, operandDictionary, start, end);
		}
		/// <summary>
		/// Matches the method's instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary,
			int start, ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(method, operandDictionary, start);
		}
		/// <summary>
		/// Matches the method's instructions to the whole pattern.
		/// </summary>
		/// <param name="method">The method containing the instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(MethodDefinition method, ILOperandDictionary operandDictionary,
			ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(method, operandDictionary);
		}
		/// <summary>
		/// Matches the  instructions to the pattern with a starting and ending range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="end">The ending range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary,
			int start, int end, ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(instructions, operandDictionary, start, end);
		}
		/// <summary>
		/// Matches the instructions to the pattern with a starting range.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="start">The starting range.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary,
			int start, ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(instructions, operandDictionary, start);
		}
		/// <summary>
		/// Matches the  instructions to the whole pattern.
		/// </summary>
		/// <param name="instructions">The instructions to match.</param>
		/// <param name="operandDictionary">The dictionary of existing operands to equate to.</param>
		/// <param name="pattern">The IL pattern to create the regex from.</param>
		/// <returns>The pattern's match, or an unseccesful match.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instructions"/>, <paramref name="operandDictionary"/>, or
		/// <paramref name="pattern"/> is null.
		/// </exception>
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILMatch Match(Instruction[] instructions, ILOperandDictionary operandDictionary,
			ILPattern pattern, ILRegexOptions options = default)
		{
			if (operandDictionary == null)
				throw new ArgumentNullException(nameof(operandDictionary));
			return new ILRegex(pattern, options).Match(instructions, operandDictionary);
		}

		#endregion
		
		#region Parsing

		/// <summary>
		/// Parses the string into <see cref="ILRegex"/>.
		/// </summary>
		/// <param name="s">The string representation of the pattern to parse.</param>
		/// <returns>The parsed regex.</returns>
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
		public static ILRegex Parse(string s) {
			return new ILRegex(new ILPattern(ILCheck.ParseMany(s)));
		}
		/// <summary>
		/// Parses the string into <see cref="ILRegex"/>.
		/// </summary>
		/// <param name="s">The string representation of the regex pattern to parse.</param>
		/// <param name="options">The matching options to use for the regex.</param>
		/// <returns>The parsed regex.</returns>
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
		public static ILRegex Parse(string s, ILRegexOptions options) {
			return new ILRegex(new ILPattern(ILCheck.ParseMany(s)), options);
		}
		/// <summary>
		/// Parses the text from a file into <see cref="ILRegex"/>.
		/// </summary>
		/// <param name="filePath">The file path to get the text from.</param>
		/// <returns>The parsed regex.</returns>
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
		public static ILRegex FromFile(string filePath) {
			return new ILRegex(ILPattern.FromFile(filePath));
		}
		/// <summary>
		/// Parses the text from a file into <see cref="ILRegex"/>.
		/// </summary>
		/// <param name="filePath">The file path to get the text from.</param>
		/// <param name="options">The matching options to use for the regex.</param>
		/// <returns>The parsed regex.</returns>
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
		public static ILRegex FromFile(string filePath, ILRegexOptions options) {
			return new ILRegex(ILPattern.FromFile(filePath), options);
		}

		#endregion
	}
}
