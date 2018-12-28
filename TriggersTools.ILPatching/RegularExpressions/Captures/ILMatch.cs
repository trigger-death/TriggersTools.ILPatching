using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A whole match for an IL Regular Expression.
	/// </summary>
	[Serializable]
	public class ILMatch : ILGroup {
		#region Constants

		/// <summary>
		/// An unsuccessful match capture.
		/// </summary>
		internal static readonly ILMatch EmptyMatch = new ILMatch();

		#endregion

		#region Fields

		/// <summary>
		/// Gets the regex used to complete this match.
		/// </summary>
		public ILRegex Regex { get; }
		/// <summary>
		/// Gets the method definition that was matched against, if there was one.
		/// </summary>
		public MethodDefinition Method  { get; }
		/// <summary>
		/// Gets the collection of captured groups. Index zero is the original match.
		/// </summary>
		public ILGroupCollection Groups { get; }
		/// <summary>
		/// Gets the collection of captured operands.
		/// </summary>
		public ILOperandCollection Operands { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs an unsuccessful match capture.
		/// </summary>
		private ILMatch() {
			Groups = new ILGroupCollection(new ILGroup[0]);
			Operands = new ILOperandCollection(new ILOperand[0]);
		}
		/// <summary>
		/// Constructs the IL Match from the matched groups and operands.
		/// </summary>
		/// <param name="regex">The entire instruction set that was matched against.</param>
		/// <param name="method">The method definition that was matched against, if there was one.</param>
		/// <param name="groups">The collection of matched groups, including this group.</param>
		/// <param name="operands">The collection of matched operands.</param>
		/// <param name="success">True if this is a successful match and not a match stating the most progress.</param>
		internal ILMatch(ILRegex regex, MethodDefinition method, ILGroup[] groups, ILOperand[] operands, bool success = true)
			: base(groups[0].Instructions, groups[0].Index, groups[0].End, null, success)
		{
			Regex = regex;
			Method = method;
			Groups = new ILGroupCollection(groups);
			Operands = new ILOperandCollection(operands);
		}

		#endregion

		#region NextMatch

		/// <summary>
		/// Returns a new Match with the results for the next match, starting at the position at which the
		/// last match ended (at the instruction beyond the last matched instruction).
		/// </summary>
		/// <returns>The new match, succesful or unsuccesful.</returns>
		public ILMatch NextMatch() {
			if (Regex == null)
				return this; // We're an empty match

			if (Method != null) // Match with a method if we have one
				return Regex.Match(Method, End);
			else
				return Regex.Match(Instructions, End);
		}
		/// <summary>
		/// Returns a new Match with the results for the next match, starting at the position at which the
		/// last match ended (at the instruction beyond the last matched instruction).
		/// </summary>
		/// <param name="end">The index after the last instruction that can be matched.</param>
		/// <returns>The new match, succesful or unsuccesful.</returns>
		public ILMatch NextMatch(int end) {
			if (Regex == null)
				return this; // We're an empty match

			if (Method != null) // Match with a method if we have one
				return Regex.Match(Method, End, end);
			else
				return Regex.Match(Instructions, End, end);
		}

		#endregion

		#region Get Operand

		/// <summary>
		/// Gets the captured operand of the specified type at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of the operand to get.</typeparam>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured operand, or default(<typeparamref name="T"/>) if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not of the specified type.
		/// </exception>
		public T GetOperand<T>(int index) {
			ILOperand operand = Operands[index];
			if (!operand.Success)
				return default;
			if (operand.Operand is T t)
				return t;
			throw new InvalidCastException($"Operand[{index}] is not of type {typeof(T).Name}, but {(operand.Operand.GetType()?.Name ?? typeof(DBNull).Name)}!");
		}
		/// <summary>
		/// Gets the captured operand of the specified type with the specified name.
		/// </summary>
		/// <typeparam name="T">The type of the operand to get.</typeparam>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured operand, or default(<typeparamref name="T"/>) if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not of the specified type.
		/// </exception>
		public T GetOperand<T>(string name) {
			ILOperand operand = Operands[name];
			if (!operand.Success)
				return default;
			if (operand.Operand is T t)
				return t;
			throw new InvalidCastException($"Operand[\"{name}\"] is not of type {typeof(T).Name}, but {(operand.Operand.GetType()?.Name ?? typeof(DBNull).Name)}!");
		}

		/// <summary>
		/// Gets the captured parameter definition operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured parameter definition, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a parameter definition.
		/// </exception>
		public ParameterDefinition GetParameter(int index) {
			return GetOperand<ParameterDefinition>(index);
		}
		/// <summary>
		/// Gets the captured parameter definition operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured parameter definition, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a parameter definition.
		/// </exception>
		public ParameterDefinition GetParameter(string name) {
			return GetOperand<ParameterDefinition>(name);
		}

		/// <summary>
		/// Gets the captured variable definition operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured variable definition, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a variable definition.
		/// </exception>
		public VariableDefinition GetVariable(int index) {
			return GetOperand<VariableDefinition>(index);
		}
		/// <summary>
		/// Gets the captured variable definition operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured variable definition, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a variable definition.
		/// </exception>
		public VariableDefinition GetVariable(string name) {
			return GetOperand<VariableDefinition>(name);
		}
		
		/// <summary>
		/// Gets the captured field reference operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured field reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a field reference.
		/// </exception>
		public FieldReference GetField(int index) {
			return GetOperand<FieldReference>(index);
		}
		/// <summary>
		/// Gets the captured field reference operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured field reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a field reference.
		/// </exception>
		public FieldReference GetField(string name) {
			return GetOperand<FieldReference>(name);
		}

		/// <summary>
		/// Gets the captured method reference operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured method reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a method reference.
		/// </exception>
		public MethodReference GetMethod(int index) {
			return GetOperand<MethodReference>(index);
		}
		/// <summary>
		/// Gets the captured method reference operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured method reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a method reference.
		/// </exception>
		public MethodReference GetMethod(string name) {
			return GetOperand<MethodReference>(name);
		}

		/// <summary>
		/// Gets the captured type reference operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured type reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a type reference.
		/// </exception>
		public MethodReference GetType(int index) {
			return GetOperand<MethodReference>(index);
		}
		/// <summary>
		/// Gets the captured type reference operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured type reference, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a type reference.
		/// </exception>
		public MethodReference GetType(string name) {
			return GetOperand<MethodReference>(name);
		}

		/// <summary>
		/// Gets the captured integer (&lt;=32bits) operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured integer, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not an integer or is greater than 32 bits.
		/// </exception>
		public int? GetInt32(int index) {
			return GetOperand<int?>(index);
		}
		/// <summary>
		/// Gets the captured integer (&lt;=32bits) operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured integer, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not an integer or is greater than 32 bits.
		/// </exception>
		public int? GetInt32(string name) {
			return GetOperand<int?>(name);
		}

		/// <summary>
		/// Gets the captured integer (&lt;=64bits) operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured integer, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not an integer.
		/// </exception>
		public long? GetInt64(int index) {
			return GetOperand<long?>(index);
		}
		/// <summary>
		/// Gets the captured integer (&lt;=64bits) operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured integer, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not an integer.
		/// </exception>
		public long? GetInt64(string name) {
			return GetOperand<long?>(name);
		}

		/// <summary>
		/// Gets the captured single-floating point operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured float, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a float.
		/// </exception>
		public float? GetSingle(int index) {
			return GetOperand<float?>(index);
		}
		/// <summary>
		/// Gets the captured single-floating point operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured float, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a float.
		/// </exception>
		public float? GetSingle(string name) {
			return GetOperand<float?>(name);
		}

		/// <summary>
		/// Gets the captured double-floating point operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured double, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a double.
		/// </exception>
		public double? GetDouble(int index) {
			return GetOperand<double?>(index);
		}
		/// <summary>
		/// Gets the captured double-floating point operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured double, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a double.
		/// </exception>
		public double? GetDouble(string name) {
			return GetOperand<double?>(name);
		}

		/// <summary>
		/// Gets the captured string operand at the specified index.
		/// </summary>
		/// <param name="index">The capture index of the operand.</param>
		/// <returns>The captured string, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a string.
		/// </exception>
		public string GetString(int index) {
			return GetOperand<string>(index);
		}
		/// <summary>
		/// Gets the captured string operand with the specified name.
		/// </summary>
		/// <param name="name">The capture name of the operand.</param>
		/// <returns>The captured string, or null if the capture was unsuccesful.</returns>
		/// 
		/// <exception cref="InvalidCastException">
		/// The captured operand is not a string.
		/// </exception>
		public string GetString(string name) {
			return GetOperand<string>(name);
		}

		#endregion
	}
}
