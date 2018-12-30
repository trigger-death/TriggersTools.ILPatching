using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// Opcodes for <see cref="ILCheck"/>.
	/// </summary>
	public enum OpChecks {
		/// <summary>No operation. Used in compiled regex to fill in empty groups.</summary>
		Nop = 0,

		// State Change
		/// <summary>Signifies the beginning of a group, optionally captures.</summary>
		GroupStart,
		/// <summary>Signifies the end of a group. Quantifiers will surround the entire group.</summary>
		GroupEnd,
		/// <summary>Splits the instructions in the current group and adds an alternate choice.</summary>
		Alternative,
		/// <summary>Start of search input.</summary>
		Start,
		/// <summary>End of search input.</summary>
		End,

		/// <summary>This opcheck is a quantifier with no attachment.</summary>
		Quantifier,
		///// <summary>Dummy Opcheck used to signify start of consuming opchecks.</summary>
		//Consuming,

		// All opchecks listed after this are "consuming opchecks"

		// Operands
		/// <summary>Captures the operand of the instruction.</summary>
		Operand,
		/// <summary>Match an existing captured operand.</summary>
		OperandEquals,
		/// <summary>Any instruction is allowed and is not checked.</summary>
		Skip,
		///// <summary>A custom method is used to check the instruction.</summary>
		//Predicate,
		/// <summary>Match just an OpCode.</summary>
		OpCode,
		/// <summary>Match an OpCode and operand.</summary>
		OpCodeOperand,
		/// <summary>Compares the name of the field reference operand.</summary>
		FieldName,
		/// <summary>Compares the name of the method reference operand.</summary>
		MethodName,
		/// <summary>Compares the name of the type reference operand.</summary>
		TypeName,
		/// <summary>Compares the name of the type reference operand.</summary>
		CallSiteName,
	}
	/// <summary>
	/// Extension methods for use with <see cref="OpChecks"/> enum.
	/// </summary>
	public static class OpChecksExtensions {
		/// <summary>
		/// Checks if this opcheck code consumes an IL instruction.
		/// </summary>
		/// <param name="code">The code to check.</param>
		/// <returns>True if the code consumes an IL instruction, otherwise false.</returns>
		public static bool IsConsuming(this OpChecks code) {
			return code >= OpChecks.Operand;
		}
	}
	/// <summary>
	/// A single check that is performed by the <see cref="ILRegex"/>.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public partial class ILCheck : IFormattable {
		#region Constants

		/// <summary>
		/// Used for <see cref="CaptureIndex"/> to specified that this is not a capturing group/operand.
		/// </summary>
		internal const int NoCapture = -1;
		/// <summary>
		/// The default format applied with <see cref="ToString"/>.
		/// </summary>
		internal const string DefaultToStringFormat = "";
		/// <summary>
		/// The default format applied with <see cref="Print"/>.
		/// </summary>
		internal const string DefaultPrintFormat = "";
		
		#endregion

		#region Fields

		/// <summary>
		/// The code of the opcheck to perform.
		/// </summary>
		public OpChecks Code { get; }
		/// <summary>
		/// The number of times this opcheck must be repeated.
		/// </summary>
		public ILQuantifier Quantifier { get; internal set; } = ILQuantifier.ExactlyOne;
		/// <summary>
		/// The index of capture for groups and operands. This value is assigned during pre-processing.<para/>
		/// If the value is <see cref="NoCapture"/>, then this is not a capturing group/operand.<para/>
		/// This value is also used when <see cref="CaptureName"/> is null for
		/// <see cref="OpChecks.OperandEquals"/>.
		/// </summary>
		public int CaptureIndex { get; internal set; } = NoCapture;
		/// <summary>
		/// The optional name used when capturing a group or operand.
		/// This value is also used for <see cref="OpChecks.OperandEquals"/> when non-null.
		/// </summary>
		public string CaptureName { get; internal set; }

		/// <summary>
		/// The opcode that must be matched with <see cref="OpChecks.OpCode"/>,
		/// <see cref="OpChecks.OpCodeOperand"/>, <see cref="OpChecks.Operand"/>, and
		/// <see cref="OpChecks.OperandEquals"/>.
		/// </summary>
		public AnyOpCode OpCode { get; internal set; }
		/// <summary>
		/// The operand that must be matched with <see cref="OpChecks.OpCodeOperand"/>.
		/// </summary>
		public object Operand { get; internal set; }

		/*/// <summary>
		/// The custom function used to check the instruction.
		/// </summary>
		public ILInstructionMethodPredicate Predicate { get; internal set; }*/

		// Assigned during compilation
		/// <summary>
		/// The index of the opcheck in the <see cref="ILRegex"/>.
		/// </summary>
		internal int OpCheckIndex { get; set; }
		/// <summary>
		/// The end or start group opcheck when this is a start or end group opcheck.
		/// </summary>
		internal ILCheck GroupOther { get; set; }
		/// <summary>
		/// The collection of alternative op codes in the group.
		/// </summary>
		internal ILCheck[] Alternatives { get; set; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets if this opcheck consumes the current IL instruction.
		/// </summary>
		public bool IsConsuming => Code.IsConsuming();
		/// <summary>
		/// Gets or sets if the opcheck should perform group or operand capture.
		/// </summary>
		public bool IsCapture {
			get => CaptureIndex != NoCapture;
			internal set => CaptureIndex = (value ? 0 : NoCapture);
		}
		/// <summary>
		/// The member name used for capturing field, method and type names. This replaces
		/// <see cref="CaptureName"/>.
		/// </summary>
		public string MemberName {
			get => CaptureName;
			internal set => CaptureName = value;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the opcheck with the specified opcode.
		/// </summary>
		/// <param name="code">The opcode specifying the type of opcheck.</param>
		internal ILCheck(OpChecks code) {
			Code = code;
			if (code != OpChecks.GroupStart && code != OpChecks.Operand)
				IsCapture = false;
		}

		#endregion

		#region Clone

		/// <summary>
		/// Clones the ILOpCheck so that it can be modified.
		/// </summary>
		/// <returns>The copy of the ILOpCheck.</returns>
		internal ILCheck Clone() {
			return (ILCheck) MemberwiseClone();
		}

		#endregion

		#region Repeat

		/// <summary>
		/// Clones and quantifies the check by exactly the specified amount.
		/// </summary>
		/// <param name="exactly">The number of times the last check must be matched.</param>
		/// <returns>The duplicated IL check to pass to <see cref="ILRegex"/>.</returns>
		public ILCheck Repeat(int exactly) {
			return Repeat(new ILQuantifier(exactly, exactly, true));
		}
		/// <summary>
		/// Clones and quantifies the check by the specified min and max range.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="min">The maximum number of times the last check can be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The duplicated IL check to pass to <see cref="ILRegex"/>.</returns>
		public ILCheck Repeat(int min, int max, bool greedy = false) {
			return Repeat(new ILQuantifier(min, max, greedy));
		}
		/// <summary>
		/// Clones and quantifies the check by as many times as it wants, with a minimum of 0.
		/// </summary>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The duplicated IL check to pass to <see cref="ILRegex"/>.</returns>
		public ILCheck RepeatIndefinite(bool greedy = false) {
			return Repeat(new ILQuantifier(0, ILQuantifier.OrMore, greedy));
		}
		/// <summary>
		/// Clones and quantifies the check by as many times as it wants, with the specified minimum.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The duplicated IL check to pass to <see cref="ILRegex"/>.</returns>
		public ILCheck RepeatIndefinite(int min, bool greedy = false) {
			return Repeat(new ILQuantifier(min, ILQuantifier.OrMore, greedy));
		}
		/// <summary>
		/// Clones and quantifies the check by the specified quantifier.
		/// </summary>
		/// <param name="quantifier">The quantifier to add to the check.</param>
		/// <returns>The duplicated IL check to pass to <see cref="ILRegex"/>.</returns>
		public ILCheck Repeat(ILQuantifier quantifier) {
			if (Code == OpChecks.GroupStart)
				throw new ILRegexException($"Cannot attach quantifier {quantifier} to group start {this}!");
			else if (Code == OpChecks.Alternative)
				throw new ILRegexException($"Cannot attach quantifier {quantifier} to altervative {this}!");
			//else if (!Quantifier.IsOne)
			//	throw new ILRegexException($"Cannot attach quantifier {quantifier} to an already quantified check {this}!");
			ILCheck check = Clone();
			check.Quantifier = quantifier;
			return check;
		}

		#endregion
	}
}
