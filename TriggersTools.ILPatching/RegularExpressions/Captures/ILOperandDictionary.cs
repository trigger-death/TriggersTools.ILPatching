using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A dictionary of named operands that can be referenced in <see cref="ILRegex"/> and used to create
	/// instructions.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ILOperandDictionary : IDictionary<string, object> {
		#region Fields

		/// <summary>
		/// The dictionary of named operands.
		/// </summary>
		private readonly Dictionary<string, object> operands;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an empty <see cref="ILOperandDictionary"/>.
		/// </summary>
		public ILOperandDictionary() {
			operands = new Dictionary<string, object>();
		}

		#endregion

		#region IDictionary Implementation

		/// <summary>
		/// Determines whether the <see cref="ILOperandDictionary"/> contains the specified name.
		/// </summary>
		/// <param name="name">The name to locate in the <see cref="ILOperandDictionary"/>.</param>
		/// <returns>
		/// True if th e<see cref="ILOperandDictionary"/> contains an operand with the specified name;
		/// otherwise, false.
		/// </returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is null.
		/// </exception>
		public bool ContainsKey(string name) => operands.ContainsKey(name);
		/// <summary>
		/// Determines whether the <see cref="ILOperandDictionary"/> contains a specific operand value.
		/// </summary>
		/// <param name="operand">
		/// The value to locate in the <see cref="ILOperandDictionary"/>. The value can be null.
		/// </param>
		/// <returns>
		/// True if the <see cref="ILOperandDictionary"/> contains an operand with the specified value;
		/// otherwise, false.
		/// </returns>
		public bool ContainsValue(object operand) => operands.ContainsValue(operand);
		
		/// <summary>
		/// Gets the operand value associated with the specified name.
		/// </summary>
		/// <param name="name">The name of the operand value to get.</param>
		/// <param name="operand">
		/// When this method returns, contains the operand value associated with the specified name, if the
		/// key is found; otherwise, the value is null.
		/// </param>
		/// <returns>
		/// True if the <see cref="ILOperandDictionary"/> contains an operand with the specified name;
		/// otherwise, false.
		/// </returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is null.
		/// </exception>
		public bool TryGetValue(string name, out object operand) => operands.TryGetValue(name, out operand);

		/// <summary>
		/// Adds the specified name and operand to the dictionary.
		/// </summary>
		/// <param name="name">The name of the operand to add.</param>
		/// <param name="operand">The value of the operand to add. The value can be null.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// An operand with the same name already exists in the <see cref="ILOperandDictionary"/>. Or
		/// <paramref name="operand"/> is not a valid operand type. Or <paramref name="name"/> is not a valid
		/// regex capture name.
		/// </exception>
		public void Add(string name, object operand) {
			IL.ThrowIfInvalidCaptureName(name, false);
			IL.ThrowIfInvalidOperandType(operand);
			operands.Add(name, operand);
		}
		/// <summary>
		/// Adds the range of specified names and operands to the dictionary.
		/// </summary>
		/// <param name="items">The name/operand pairs to add.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="items"/> or a <see cref="KeyValuePair{TKey, TValue}.Key"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// An operand with the same <see cref="KeyValuePair{TKey, TValue}.Key"/> already exists in the
		/// <see cref="ILOperandDictionary"/>. Or <see cref="KeyValuePair{TKey, TValue}.Key"/> is not a valid
		/// regex capture name.
		/// </exception>
		public void AddRange(IEnumerable<KeyValuePair<string, object>> items) {
			foreach (var pair in items)
				Add(pair.Key, pair.Value);
		}
		/// <summary>
		/// Adds the range of named matched operands to the dictionary.
		/// </summary>
		/// <param name="match">The match whose named operands will be added.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="match"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// An operand with the same name as in <paramref name="match"/> already exists in the
		/// <see cref="ILOperandDictionary"/>.
		/// </exception>
		public void AddMatch(ILMatch match) {
			AddOperands(match.Operands);
		}
		/// <summary>
		/// Adds the collection of named matched operands to the dictionary.
		/// </summary>
		/// <param name="operands">The collection whose named operands will be added.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="operands"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// An operand with the same name as in <paramref name="match"/> already exists in the
		/// <see cref="ILOperandDictionary"/>.
		/// </exception>
		public void AddOperands(ILOperandCollection operands) {
			foreach (ILOperand operand in operands) {
				if (operand.Name != null) // Implicitly means the operand was successful
					this.operands.Add(operand.Name, operand.Operand);
			}
		}
		/// <summary>
		/// Adds the specified named operand to the dictionary.
		/// </summary>
		/// <param name="operand">The named operand to add.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="operand"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// An operand with the same name already exists in the <see cref="ILOperandDictionary"/>. Or
		/// the operand is unnamed or unsecessful.
		/// </exception>
		public void AddOperand(ILOperand operand) {
			if (operand.Name != null) // Implicitly means the operand was successful
				operands.Add(operand.Name, operand.Operand);
			else
				throw new ArgumentException($"Cannot add unnamed {nameof(ILOperand)} to {nameof(ILOperandDictionary)}!");
		}
		/// <summary>
		/// Removes the value with the specified key from the System.Collections.Generic.Dictionary`2.
		/// </summary>
		/// <param name="name">The name of the operand to remove.</param>
		/// <returns>
		/// True if the operand is successfully found and removed; otherwise, false. This method returns
		/// false if key is not found in the <see cref="ILOperandDictionary"/>.
		/// </returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is null.
		/// </exception>
		public bool Remove(string name) => operands.Remove(name);
		/// <summary>
		/// Removes all names and operands from the <see cref="ILOperandDictionary"/>.
		/// </summary>
		public void Clear() => operands.Clear();

		/// <summary>
		/// Gets or sets the value associated with the specified name.
		/// </summary>
		/// <param name="name">The name of the operand to get or set.</param>
		/// <returns>
		/// The operand associated with the specified name. If the specified name is not found, a get
		/// operation throws a <see cref="KeyNotFoundException"/>, and a set operation creates a new operand
		/// with the specified name.
		/// </returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="name"/> is null.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// The property is retrieved and <paramref name="name"/> does not exist in the collection.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// This is a set operation and <paramref name="operand"/> is not a valid operand type. Or this is a
		/// set operation and <paramref name="name"/> is not a valid regex capture name.
		/// </exception>
		public object this[string name] {
			get => operands[name];
			set {
				IL.ThrowIfInvalidCaptureName(name, false);
				IL.ThrowIfInvalidOperandType(value);
				operands[name] = value;
			}
		}
		/// <summary>
		/// Gets the number of name/operand pairs contained in the <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <returns>
		/// The number of name/operand pairs contained in the <see cref="ILOperandDictionary"/>.
		/// </returns>
		public int Count => operands.Count;
		/// <summary>
		/// Gets a collection containing the names in the <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="Dictionary{TKey, TValue}.KeyCollection"/> containing the names in the
		/// <see cref="ILOperandDictionary"/>.
		/// </returns>
		public ICollection<string> Keys => operands.Keys;
		/// <summary>
		/// Gets a collection containing the operands in the <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="Dictionary{TKey, TValue}.ValueCollection"/> containing the operands in the
		/// <see cref="ILOperandDictionary"/>.
		/// </returns>
		public ICollection<object> Values => operands.Values;
		bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;
		
		/// <summary>
		/// Returns an enumerator that iterates through the <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="Dictionary{TKey, TValue}.Enumerator"/> structure for the <see cref="ILOperandDictionary"/>.
		/// </returns>
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => operands.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		
		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) {
			((ICollection<KeyValuePair<string, object>>) operands).Add(item);
		}
		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) {
			return ((ICollection<KeyValuePair<string, object>>) operands).Contains(item);
		}
		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
			((ICollection<KeyValuePair<string, object>>) operands).CopyTo(array, arrayIndex);
		}
		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) {
			return ((ICollection<KeyValuePair<string, object>>) operands).Remove(item);
		}

		#endregion

		/*#region CreateInstruction

		/// <summary>
		/// Creates an <see cref="Instruction"/> using the operand with the specified name in the
		/// <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <param name="opcode">The opcode of the instruction.</param>
		/// <param name="operandName">The name of the operand to get.</param>
		/// <returns>The created instruction.</returns>
		public Instruction CreateInstruction(OpCode opcode, string operandName) {
			return IL.CreateInstruction(opcode, operands[operandName]);
		}
		
		#endregion*/

		#region DebuggerDisplay

		private string DebuggerDisplay => $"Count = {Count}";

		#endregion
	}
}
