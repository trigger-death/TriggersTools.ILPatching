using System;
using System.Collections.Generic;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A static class for constucting <see cref="ILCheck"/>s for <see cref="ILRegex"/> patterns.
	/// </summary>
	public static class ILChecks {
		#region No Arguments

		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for the start of the instructions.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Start => new ILCheck(OpChecks.Start);
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for the end of the instructions.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck End => new ILCheck(OpChecks.End);
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for the an alternative group to match.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Alternative => new ILCheck(OpChecks.Alternative);
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group start to match the following checks.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck GroupStart => new ILCheck(OpChecks.GroupStart);
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for the ending of a group.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck GroupEnd => new ILCheck(OpChecks.GroupEnd);
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check that matches any instruction
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Any => new ILCheck(OpChecks.Skip);

		#endregion

		#region OpCode

		/// <summary>
		/// Creates an <see cref="ILRegex"/> check just for matching opcodes.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck OpCode(AnyOpCode opCode) {
			return new ILCheck(OpChecks.OpCode) { OpCode = opCode };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for matching opcodes and operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="operand">The operand to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck OpCode(AnyOpCode opCode, object operand) {
			return new ILCheck(OpChecks.OpCodeOperand) { OpCode = opCode, Operand = operand };
		}

		#endregion

		#region Operand

		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for capturing the value of an operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck CaptureOperand(AnyOpCode opCode) {
			return new ILCheck(OpChecks.Operand) { OpCode = opCode, IsCapture = true };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for capturing the value of an operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="captureName">The name of the capture.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck CaptureOperand(AnyOpCode opCode, string captureName) {
			IL.ThrowIfInvalidCaptureName(captureName, false);
			return new ILCheck(OpChecks.Operand) { OpCode = opCode, CaptureName = captureName, IsCapture = true };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for comparing the value of an already-captured operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="captureName">The capture name of the operand.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck EqualsOperand(AnyOpCode opCode, string captureName) {
			IL.ThrowIfInvalidCaptureName(captureName, false);
			return new ILCheck(OpChecks.OperandEquals) { OpCode = opCode, CaptureName = captureName };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> check for comparing the value of an already-captured operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="captureIndex">The capture index of the operand.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck EqualsOperand(AnyOpCode opCode, int captureIndex) {
			return new ILCheck(OpChecks.OperandEquals) { OpCode = opCode, CaptureIndex = captureIndex };
		}

		#endregion

		#region Group

		/*/// <summary>
		/// Creates an <see cref="ILRegex"/> group start to match the following checks.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static IILCheck GroupStart() {
			return new ILOpCheck(OpChecks.GroupStart) { IsCapture = false };
		}*/
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group start to capture the following checks.
		/// </summary>
		/// <param name="checks">The checks to group together.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck CaptureGroupStart() {
			return new ILCheck(OpChecks.GroupStart) { IsCapture = true };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group start to capture the following checks.
		/// </summary>
		/// <param name="captureName">The capture name of the group.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck CaptureGroupStart(string captureName) {
			IL.ThrowIfInvalidCaptureName(captureName, false);
			return new ILCheck(OpChecks.GroupStart) { CaptureName = captureName, IsCapture = true };
		}
		
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group of checks match.
		/// </summary>
		/// <param name="checks">The checks to group together.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static IEnumerable<ILCheck> Group(params ILCheck[] checks) {
			return Group(false, null, checks);
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group of checks to capture.
		/// </summary>
		/// <param name="checks">The checks to group together.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static IEnumerable<ILCheck> CaptureGroup(params ILCheck[] checks) {
			return Group(true, null, checks);
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> group of checks to capture.
		/// </summary>
		/// <param name="captureName">The capture name of the group.</param>
		/// <param name="checks">The checks to group together.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static IEnumerable<ILCheck> CaptureGroup(string captureName, params ILCheck[] checks) {
			IL.ThrowIfInvalidCaptureName(captureName, false);
			return Group(true, captureName, checks);
		}

		private static IEnumerable<ILCheck> Group(bool capture, string captureName, ILCheck[] checks) {
			yield return new ILCheck(OpChecks.GroupStart) {
				IsCapture = capture,
				CaptureName = captureName,
			};
			foreach (ILCheck check in checks)
				yield return check;
			yield return new ILCheck(OpChecks.GroupEnd);
		}

		#endregion

		#region Match Member Name

		/// <summary>
		/// Creates an <see cref="ILRegex"/> to compare the name of a field operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="name">The name of the field operand to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Field(AnyOpCode opCode, string name) {
			return new ILCheck(OpChecks.FieldName) { OpCode = opCode, MemberName = name, };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> to compare the name of a method operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="name">The name of the method operand to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Method(AnyOpCode opCode, string name) {
			return new ILCheck(OpChecks.MethodName) { OpCode = opCode, MemberName = name, };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> to compare the name of a type operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="name">The name of the type operand to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Type(AnyOpCode opCode, string name) {
			return new ILCheck(OpChecks.TypeName) { OpCode = opCode, MemberName = name, };
		}
		/// <summary>
		/// Creates an <see cref="ILRegex"/> to compare the name of a callsite operand.
		/// </summary>
		/// <param name="opCode">The opcode to match.</param>
		/// <param name="name">The name of the callsite operand to match.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck CallSite(AnyOpCode opCode, string name) {
			return new ILCheck(OpChecks.CallSiteName) { OpCode = opCode, MemberName = name, };
		}

		#endregion

		#region Repeat

		/// <summary>
		/// Quantifies the last check by exactly the specified amount.
		/// </summary>
		/// <param name="exactly">The number of times the last check must be matched.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Repeat(int exactly) {
			return new ILCheck(OpChecks.Quantifier) { Quantifier = new ILQuantifier(exactly) };
		}
		/// <summary>
		/// Quantifies the last check by the specified min and max range.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="min">The maximum number of times the last check can be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Repeat(int min, int max, bool greedy = false) {
			return new ILCheck(OpChecks.Quantifier) { Quantifier = new ILQuantifier(min, max, greedy) };
		}
		/// <summary>
		/// Quantifies the last check by as many times as it wants, with a minimum of 0.
		/// </summary>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck RepeatIndefinite(bool greedy = false) {
			return new ILCheck(OpChecks.Quantifier) { Quantifier = new ILQuantifier(0, ILQuantifier.OrMore, greedy) };
		}
		/// <summary>
		/// Quantifies the last check by as many times as it wants, with the specified minimum.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck RepeatIndefinite(int min, bool greedy = false) {
			return new ILCheck(OpChecks.Quantifier) { Quantifier = new ILQuantifier(min, ILQuantifier.OrMore, greedy) };
		}
		/// <summary>
		/// Quantifies the last check with the specified quantifier.
		/// </summary>
		/// <param name="quantifier">The quantifier to use for the last check.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Repeat(ILQuantifier quantifier) {
			return new ILCheck(OpChecks.Quantifier) { Quantifier = quantifier };
		}

		#endregion

		#region Skip

		/// <summary>
		/// Skips the next instructions once.
		/// </summary>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Skip() {
			return new ILCheck(OpChecks.Skip);
		}
		/// <summary>
		/// Skips the next instructions by exactly the specified amount.
		/// </summary>
		/// <param name="exactly">The number of times the last check must be matched.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Skip(int exactly) {
			return new ILCheck(OpChecks.Skip) { Quantifier = new ILQuantifier(exactly) };
		}
		/// <summary>
		/// Skips the next instructions by the specified min and max range.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="min">The maximum number of times the last check can be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Skip(int min, int max, bool greedy = false) {
			return new ILCheck(OpChecks.Skip) { Quantifier = new ILQuantifier(min, max, greedy) };
		}
		/// <summary>
		/// Skips the next instructions by as many times as it wants, with a minimum of 0.
		/// </summary>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck SkipIndefinite(bool greedy = false) {
			return new ILCheck(OpChecks.Skip) { Quantifier = new ILQuantifier(0, ILQuantifier.OrMore, greedy) };
		}
		/// <summary>
		/// Skips the next instructions by as many times as it wants, with the specified minimum.
		/// </summary>
		/// <param name="min">The minimum number of times the last check must be matched.</param>
		/// <param name="greedy">True if the quantifier is greedy.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck SkipIndefinite(int min, bool greedy = false) {
			return new ILCheck(OpChecks.Skip) { Quantifier = new ILQuantifier(min, ILQuantifier.OrMore, greedy) };
		}
		/// <summary>
		/// kips the next instructions by the specified quantifier.
		/// </summary>
		/// <param name="quantifier">The quantifier to use for skipping.</param>
		/// <returns>The IL check to pass to <see cref="ILPatternBuilder"/>.</returns>
		public static ILCheck Skip(ILQuantifier quantifier) {
			return new ILCheck(OpChecks.Skip) { Quantifier = quantifier };
		}

		#endregion
	}
}
