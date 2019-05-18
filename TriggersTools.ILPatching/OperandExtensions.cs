using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// Extension methods for acquiring the operand of an instruction, even when the instruction doesn't
	/// store the operand using conventional methods.
	/// </summary>
	public static class OperandExtensions {
		#region GetOperand

		/// <summary>
		/// Get the operand from the instruction.
		/// </summary>
		/// <param name="instruction">The instruction to get the opcode and operand from.</param>
		/// <param name="method">The method the instruction is called from.</param>
		/// <returns>The operand for the instruction.</returns>
		/// 
		/// <remarks>
		/// When <paramref name="method"/> is null, parameters and variables cannot be acquired.
		/// </remarks>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instruction"/> is null.
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The <paramref name="instruction"/> tried to access a non-existant parameter from the
		/// <paramref name="method"/>.
		/// </exception>
		public static object GetOperand(this Instruction instruction, MethodDefinition method) {
			if (instruction.Operand == null) {
				object operand;
				operand = GetInt(instruction, false);
				if (operand != null) return (int) operand;

				// We can't get parameters or variables, just return the operand (which is null).
				if (method == null) return null;

				operand = GetParameter(instruction, method, false);
				if (operand != null) return operand;

				operand = GetVariable(instruction, method, false);
				if (operand != null) return operand;
			}
			return instruction.Operand;
		}

		#endregion

		#region Special Operands

		/// <summary>
		/// Gets the referenced method parameter from the instruction.
		/// </summary>
		/// <param name="instruction">The instruction to get the opcode and operand from.</param>
		/// <param name="method">The method the instruction is called from.</param>
		/// <param name="includeOperand">
		/// If false, the method will not try to get the parameter from the operand.
		/// </param>
		/// <returns>The parameter definition if one exists, otherwise null.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="instruction"/> is null.
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The <paramref name="instruction"/> tried to access a non-existant parameter from the
		/// <paramref name="method"/>.
		/// </exception>
		public static ParameterDefinition GetParameter(this Instruction instruction, MethodDefinition method,
			bool includeOperand = false)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (instruction == null)
				throw new ArgumentNullException(nameof(instruction));
			Code code = instruction.OpCode.Code;

			// Method parameters
			if (method.HasThis && !method.ExplicitThis) {
				if (code == Code.Ldarg_0)
					return method.Body.ThisParameter;
				else if (code >= Code.Ldarg_1 && code <= Code.Ldarg_3)
					return method.Parameters[code - Code.Ldarg_1];
			}
			else if (code >= Code.Ldarg_0 && code <= Code.Ldarg_3)
				return method.Parameters[code - Code.Ldarg_0];

			if (!includeOperand)
				return null;

			// Try to return an operand-defined parameter.
			return instruction.Operand as ParameterDefinition;
		}
		/// <summary>
		/// Gets the referenced local variable from the instruction.
		/// </summary>
		/// <param name="instruction">The instruction to get the opcode and operand from.</param>
		/// <param name="method">The method the instruction is called from.</param>
		/// <param name="includeOperand">
		/// If false, the method will not try to get the parameter from the operand.
		/// </param>
		/// <returns>The variable definition if one exists, otherwise null.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="method"/> or <paramref name="instruction"/> is null.
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The <paramref name="instruction"/> tried to access a non-existant local variable from the
		/// <paramref name="method"/>.
		/// </exception>
		public static VariableDefinition GetVariable(this Instruction instruction, MethodDefinition method,
			bool includeOperand = false)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (instruction == null)
				throw new ArgumentNullException(nameof(instruction));
			Code code = instruction.OpCode.Code;

			// Method load local variables
			if (code >= Code.Ldloc_0 && code <= Code.Ldloc_3)
				return method.Body.Variables[code - Code.Ldloc_0];

			// Method store local variables
			if (code >= Code.Stloc_0 && code <= Code.Stloc_3)
				return method.Body.Variables[code - Code.Stloc_0];

			if (!includeOperand)
				return null;

			// Try to return an operand-defined local variable
			return instruction.Operand as VariableDefinition;
		}
		/// <summary>
		/// Gets the integer from the instruction.
		/// </summary>
		/// <param name="instruction">The instruction to get the opcode and operand from.</param>
		/// <param name="includeOperand">
		/// If false, the method will not try to get the integer from the operand.
		/// </param>
		/// <returns>The integer if one exists, otherwise null.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instruction"/> is null.
		/// </exception>
		public static int? GetInt(this Instruction instruction, bool includeOperand = false) {
			if (instruction == null)
				throw new ArgumentNullException(nameof(instruction));
			Code code = instruction.OpCode.Code;

			// Opcode-defined integer
			if (code >= Code.Ldc_I4_M1 && code <= Code.Ldc_I4_8)
				return (code - Code.Ldc_I4_0); // M1 is before 0, so we properly get the value -1.

			if (!includeOperand)
				return null;

			// Try to return an operand-defined integer
			return (instruction.Operand as int? ?? instruction.Operand as byte?) ?? instruction.Operand as sbyte?;
		}

		#endregion
	}
}
