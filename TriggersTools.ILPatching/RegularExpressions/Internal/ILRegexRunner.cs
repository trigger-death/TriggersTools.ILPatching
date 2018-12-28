using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// The class responsible for handling the <see cref="ILRegex"/> matching.
	/// </summary>
	internal partial class ILRegexRunner {
		#region Fields

		/// <summary>
		/// The IL regular expression being called.
		/// </summary>
		private readonly ILRegex regex;
		/// <summary>
		/// The method this Regex is searching in.
		/// </summary>
		private readonly MethodDefinition method;
		/// <summary>
		/// The IL processor for the method.
		/// </summary>
		private readonly ILProcessor processor;
		/// <summary>
		/// The instruction set for the method.
		/// </summary>
		private readonly Instruction[] instructions;
		/// <summary>
		/// The starting search position in the instruction set.
		/// </summary>
		private readonly int start;
		/// <summary>
		/// The ending search position in the instruction set.
		/// </summary>
		private readonly int end;
		/// <summary>
		/// The matching options.
		/// </summary>
		private readonly ILRegexOptions options;
		/// <summary>
		/// The opchecks for the regex.
		/// </summary>
		private readonly ILCheck[] opChecks;
		/// <summary>
		/// The group level of the regex. Level is always 1 or greater.
		/// </summary>
		private int level;
/*#if DEBUG
		/// <summary>
		/// The farthest opcheck that was reached with a succesful match. This is used with
		/// <see cref="ILRegexOptions.ClosestUnsuccessfulMatch"/>.
		/// </summary>
		private int farthestSuccesfulOpCheck = -1;
		/// <summary>
		/// True if the second loop is being run to capture the farthest succesful match.
		/// </summary>
		private bool secondLoop;
#endif*/

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the regex runner with the search input and an optional method/processor, and an
		/// instruction set.
		/// </summary>
		/// <param name="regex">The regex calling this runner.</param>
		/// <param name="method">The method containing the instructions.</param>
		/// <param name="processor">The IL Processor acquired from the method definition.</param>
		/// <param name="instructions">The instruction set input.</param>
		/// <param name="start">The starting search position in the instruction set.</param>
		/// <param name="end">The ending search position in the instruction set.</param>
		private ILRegexRunner(ILRegex regex, MethodDefinition method, ILProcessor processor,
			Instruction[] instructions, int? start, int? end)
		{
			if (start.HasValue) {
				if (start.Value < 0)
					throw new ArgumentOutOfRangeException(nameof(start), "start cannot be less than zero!");
				else if (start.Value > instructions.Length)
					throw new ArgumentOutOfRangeException(nameof(start), "start cannot be greater than instruction count!");
				else if (end.HasValue && start.Value > end.Value)
					throw new ArgumentOutOfRangeException(nameof(start), "start cannot be greater than end!");
			}
			if (end.HasValue) {
				if (end.Value < 0)
					throw new ArgumentOutOfRangeException(nameof(end), "end cannot be less than zero!");
				else if (end.Value > instructions.Length)
					throw new ArgumentOutOfRangeException(nameof(end), "end cannot be greater than instruction count!");
			}

			this.regex = regex;
			this.method = method;
			this.processor = processor;
			this.instructions = instructions;
			this.start = start ?? 0;
			this.end = end ?? this.instructions.Length;
			options = regex.Options;
			opChecks = regex.CompiledOpChecks;

			level = 0;
		}

		#endregion

		#region Run

		/// <summary>
		/// Runs the IL regex with the search input and method definition.
		/// </summary>
		/// <param name="regex">The regex calling this runner.</param>
		/// <param name="method">The method containing the instructions.</param>
		/// <param name="start">The starting search position in the instruction set.</param>
		/// <param name="end">The ending search position in the instruction set.</param>
		/// <returns>
		/// The IL match, <see cref="ILGroup.Success"/> determines if the match was succesful.
		/// </returns>
		public static ILMatch Run(ILRegex regex, MethodDefinition method, int? start, int? end) {
			ILProcessor processor = method.Body.GetILProcessor();
			Instruction[] instructions = processor.Body.Instructions.ToArray();
			return new ILRegexRunner(regex, method, processor, instructions, start, end).Run();
		}
		/// <summary>
		/// Runs the IL regex with the search input and instruction set.
		/// </summary>
		/// <param name="regex">The regex calling this runner.</param>
		/// <param name="instructions">The instruction set input.</param>
		/// <param name="start">The starting search position in the instruction set.</param>
		/// <param name="end">The ending search position in the instruction set.</param>
		/// <returns>
		/// The IL match, <see cref="ILGroup.Success"/> determines if the match was succesful.
		/// </returns>
		public static ILMatch Run(ILRegex regex, Instruction[] instructions, int? start, int? end) {
			return new ILRegexRunner(regex, null, null, instructions, start, end).Run();
		}
		/// <summary>
		/// Runs the IL regex pattern and attempts to match the input.
		/// </summary>
		/// <returns>
		/// The IL match, <see cref="ILGroup.Success"/> determines if the match was succesful.
		/// </returns>
		private ILMatch Run() {

			MatchState initialState = new MatchState {
				OpCheck  = opChecks[0], // RegexStart
				Started  = true,
			};
			QuantifierMatch initialMatch = new QuantifierMatch {
				End      = start,
				Groups   = new ILGroup  [regex.GroupCount],
				Operands = new ILOperand[regex.OperandCount],
			};
			for (int i = 0; i < regex.GroupCount; i++)
				initialMatch.Groups[i] = ILGroup.EmptyGroup;
			for (int i = 0; i < regex.OperandCount; i++)
				initialMatch.Operands[i] = ILOperand.EmptyOperand;
			initialState.Matches.Push(initialMatch);

			MatchState rootGroup = new MatchState {
				OpCheck  = opChecks[1], // GroupStart
				PreviousState = initialState,
			};

			for (int i = start; i < end; i++) {
				initialMatch.End = i;
				level = 0;
				if (MatchGroup(rootGroup)) {
					if (level != 0)
						Console.Write("");
					var lastMatch = rootGroup.Matches.Peek();
					return new ILMatch(regex, method, lastMatch.Groups, lastMatch.Operands);
				}
			}
			return ILMatch.EmptyMatch;
		}

		#endregion
	}
}
