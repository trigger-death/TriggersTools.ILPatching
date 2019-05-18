using System;
using System.Diagnostics;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A captured IL Regex instruction group with information about its match.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ILGroup {
		#region Constants

		/// <summary>
		/// An unsuccessful group capture.
		/// </summary>
		internal static readonly ILGroup EmptyGroup = new ILGroup();

		#endregion

		#region Fields

		/// <summary>
		/// The entire instruction set matched against the IL expression.
		/// </summary>
		internal Instruction[] Instructions { get; private protected set; }

		/// <summary>
		/// Gets the optional name of the group.
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Indicates whether the match is successful.
		/// </summary>
		public bool Success { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an unsuccessful group capture.
		/// </summary>
		private protected ILGroup() {
			Instructions = null;
			Start = 0;
			End = 0;
			Name = null;
			Success = false;
		}
		/// <summary>
		/// Constructs a successful group capture.
		/// </summary>
		/// <param name="instructions">The entire instruction set that was matched against.</param>
		/// <param name="start">The index of the first instruction in the match.</param>
		/// <param name="end">The index after the last instruction in the match.</param>
		/// <param name="name">The optional name of the group, otherwise null.</param>
		/// <param name="success">True if this is a successful match and not a match stating the most progress.</param>
		internal ILGroup(Instruction[] instructions, int start, int end, string name, bool success = true) {
			Instructions = instructions;
			Start = start;
			End = end;
			Name = name;
			Success = success;
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

		#region DebuggerDisplay

		private string DebuggerDisplay => (Success ? $"Length = {Length}" : "No Capture");

		#endregion
	}
}
