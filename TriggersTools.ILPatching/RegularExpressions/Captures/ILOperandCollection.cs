using System;
using System.Collections;
using System.Collections.Generic;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A collection of matched IL instruction operands with optional names.
	/// </summary>
	[Serializable]
	public class ILOperandCollection : IReadOnlyList<ILOperand>  {
		#region Fields
		
		/// <summary>
		/// The array of captured instruction operands.
		/// </summary>
		private readonly ILOperand[] operands;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the collection from an array.
		/// </summary>
		/// <param name="operands">The array of operands to use.</param>
		internal ILOperandCollection(ILOperand[] operands) {
			this.operands = operands;
		}

		#endregion

		#region IReadOnlyList Implementation

		/// <summary>
		/// Gets the operand group at the specified index.
		/// </summary>
		/// <param name="index">The index of the matched operand group.</param>
		/// <returns>The matched operand group if it exists, otherwise an unsuccessful operand group.</returns>
		public ILOperand this[int index] {
			get => (index >= 0 && index < Count ? operands[index] : ILOperand.EmptyOperand);
		}
		/// <summary>
		/// Gets the operand group with the specified name.
		/// </summary>
		/// <param name="name">The name of the matched operand group.</param>
		/// <returns>The matched operand group if it exists, otherwise an unsuccessful operand group.</returns>
		public ILOperand this[string name] {
			get {
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				else if (int.TryParse(name, out int index))
					return this[index];
				return Array.Find(operands, g => g.Name == name) ?? ILOperand.EmptyOperand;
			}
		}
		/// <summary>
		/// Gets the number of matched operand groups.
		/// </summary>
		public int Count => operands.Length;
		/// <summary>
		/// Gets the enumerator for the IL operand groups collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<ILOperand> GetEnumerator() => ((IEnumerable<ILOperand>) operands).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion
	}
}
