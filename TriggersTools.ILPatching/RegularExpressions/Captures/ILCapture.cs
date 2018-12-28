using System;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions.Captures {
	/// <summary>
	/// A base type for all captures of instructions.
	/// </summary>
	[Serializable]
	public class ILCapture {
		#region Fields

		/// <summary>
		/// The entire instruction set matched against the IL expression.
		/// </summary>
		internal Instruction[] Instructions { get; private protected set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an IL instruction capture.
		/// </summary>
		/// <param name="instructions">The entire instruction set that was matched against.</param>
		/// <param name="start">The index of the first instruction in the match.</param>
		/// <param name="end">The index after the last instruction in the match.</param>
		internal ILCapture(Instruction[] instructions, int start, int end) {
			Instructions = instructions;
			Start = start;
			End = end;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the captured instruction set.
		/// </summary>
		public Instruction[] Value {
			get {
				Instruction[] result = new Instruction[Length];
				if (Length != 0)
					Array.Copy(Instructions, Index, result, 0, Length);
				return result;
			}
		}
		/// <summary>
		/// Gets the starting index of the capture in the instruction set. Same as <see cref="Start"/>.
		/// </summary>
		public int Index => Start;
		/// <summary>
		/// Gets the length of the capture in the instruction set.
		/// </summary>
		public int Length => End - Start;
		/// <summary>
		/// Gets the starting index of the capture in the instruction set. Same as <see cref="Index"/>.
		/// </summary>
		public int Start { get; private protected set; }
		/// <summary>
		/// Gets the ending index of the capture in the instruction set.
		/// </summary>
		public int End { get; private protected set; }

		#endregion
	}
}
