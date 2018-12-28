using System;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A captured IL Regex instruction opreand with information about its match and the associated operand.
	/// </summary>
	[Serializable]
	public sealed class ILOperand : ILGroup {
		#region Constants

		/// <summary>
		/// An unsuccessful operand capture.
		/// </summary>
		internal static readonly ILOperand EmptyOperand = new ILOperand();

		#endregion

		#region Fields

		/// <summary>
		/// Gets the captured operand associated with this capture.
		/// </summary>
		public object Operand { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an unsuccessful operand capture.
		/// </summary>
		private ILOperand() { }
		/// <summary>
		/// Constructs a successful operand capture.
		/// </summary>
		/// <param name="instructions">The entire instruction set that was matched against.</param>
		/// <param name="index">The index of the only instruction in the match.</param>
		/// <param name="name">The optional name of the group, otherwise null.</param>
		/// <param name="operand">The captured operand.</param>
		internal ILOperand(Instruction[] instructions, int index, string name, object operand)
			: base(instructions, index, index + 1, name)
		{
			Operand = operand;
		}

		#endregion

		#region Operand

		/// <summary>
		/// Gets the captured operand associated with this group and casts it to <typeparamref name="T"/>.
		/// </summary>
		/// <returns>The operand as type <typeparamref name="T"/>.</returns>
		public T ToOperand<T>() => (T) Operand;

		#endregion
	}
}
