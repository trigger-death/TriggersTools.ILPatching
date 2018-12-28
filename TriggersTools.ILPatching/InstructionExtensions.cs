using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// Extension methods revolving around see <see cref="Instruction"/>s.
	/// </summary>
	public static class InstructionExtensions {
		#region CloneInstruction

		/// <summary>
		/// Creates a copy of an instruction.
		/// </summary>
		/// <param name="original">The original instruction to copy.</param>
		/// <returns>The duplicated instruction.</returns>
		public static Instruction CloneInstruction(this Instruction original) {
			switch (original.Operand) {
			// No operand
			case null:
				return Instruction.Create(original.OpCode);

			// Basic operands
			case int value:
				return Instruction.Create(original.OpCode, value);
			case long value:
				return Instruction.Create(original.OpCode, value);
			case byte value:
				return Instruction.Create(original.OpCode, value);
			case sbyte value:
				return Instruction.Create(original.OpCode, value);
			case float value:
				return Instruction.Create(original.OpCode, value);
			case double value:
				return Instruction.Create(original.OpCode, value);
			case string value:
				return Instruction.Create(original.OpCode, value);

			// Ref-a-Def-CallSite operands
			case ParameterDefinition parameter:
				return Instruction.Create(original.OpCode, parameter);
			case VariableDefinition variable:
				return Instruction.Create(original.OpCode, variable);
			case FieldReference field:
				return Instruction.Create(original.OpCode, field);
			case MethodReference method:
				return Instruction.Create(original.OpCode, method);
			case TypeReference type:
				return Instruction.Create(original.OpCode, type);
			case CallSite site:
				return Instruction.Create(original.OpCode, site);

			// Nested instruction operands
			case Instruction target:
				return Instruction.Create(original.OpCode, target.CloneInstruction());
			case Instruction[] targets:
				return Instruction.Create(original.OpCode, targets.CloneInstructions());

			// You're not supposed to be here
			default:
				throw new Exception($"CloneInstruction(): Unknown operand type '{original.Operand.GetType().FullName}'!");
			}
		}
		/// <summary>
		/// Creates a copy of an array of instructions.
		/// </summary>
		/// <param name="originals">The original instructions to copy.</param>
		/// <returns>The duplicated array of instructions.</returns>
		public static Instruction[] CloneInstructions(this Instruction[] originals) {
			Instruction[] clonedInstructions = new Instruction[originals.Length];

			for (int i = 0; i < originals.Length; i++)
				clonedInstructions[i] = originals[i].CloneInstruction();

			return clonedInstructions;
		}

		#endregion

		#region EqualsInstruction

		/// <summary>
		/// Returns true if the two instructions are equal in both opcode and operator.
		/// </summary>
		/// <param name="instruction">The first instruction to compare.</param>
		/// <param name="other">The second instruction to compare.</param>
		/// <returns>True if the instructions are equal.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instruction"/> or <paramref name="other"/> is null.
		/// </exception>
		public static bool EqualsInstruction(this Instruction instruction, Instruction other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			return instruction.EqualsInstruction(null, other.OpCode, other.Operand);
		}
		/// <summary>
		/// Returns true if the instruction and opcode/operator pair are equal.
		/// </summary>
		/// <param name="instruction">The instruction to compare.</param>
		/// <param name="opCode">The opcode to compare to the instruction.</param>
		/// <param name="operand">The operand to compare to the instruction.</param>
		/// <returns>True if the instructions are equal.</returns>
		/// 
		/// <remarks>
		/// When <see cref="AnyOpCode.IsMulti"/> is true, the integer operand will try to be extracted if the
		/// instruction's opcode is ldc.i4.*.
		/// </remarks>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instruction"/> is null.
		/// </exception>
		public static bool EqualsInstruction(this Instruction instruction, AnyOpCode opCode, object operand) {
			return instruction.EqualsInstruction(null, opCode, operand);
		}
		/// <summary>
		/// Returns true if the instruction and opcode/operator pair are equal.
		/// </summary>
		/// <param name="instruction">The instruction to compare.</param>
		/// <param name="method">
		/// The method used for extracting parameters and variables incase <see cref="AnyOpCode.IsMulti"/>.
		/// </param>
		/// <param name="opCode">The opcode to compare to the instruction.</param>
		/// <param name="operand">The operand to compare to the instruction.</param>
		/// <returns>True if the instructions are equal.</returns>
		/// 
		/// <remarks>
		/// When <see cref="AnyOpCode.IsMulti"/> is true, the integer operand will try to be extracted if the
		/// instruction's opcode is ldc.i4.#, and parameter and variable operands will try to be extracted
		/// if the instruction's opcode is ldarg.#, ldloc.#, or stloc.#.
		/// <para/>
		/// When <paramref name="method"/> is null, parameters and variables cannot be extracted.
		/// </remarks>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="instruction"/> is null.
		/// </exception>
		public static bool EqualsInstruction(this Instruction instruction, MethodDefinition method, AnyOpCode opCode, object operand) {
			if (instruction == null)
				throw new ArgumentNullException(nameof(instruction));
			OpCode thisOpCode = instruction.OpCode;
			object thisOperand = instruction.Operand;
			if (opCode.IsMulti)
				thisOperand = instruction.GetOperand(method); // method == null returns instruction.Operand 
			if (opCode != thisOpCode)
				return false;
			
			if (operand == null || thisOperand == null)
				return (operand == null && thisOperand == null);

			// Resolve variable/parameter indecies to definitions
			if (method != null && operand is int operandIndex) {
				if (opCode.IsParameterOpCode)
					operand = method.Parameters[operandIndex];
				else if (opCode.IsVariableOpCode)
					operand = method.Body.Variables[operandIndex];
			}

			// We compare primitives differently for the sake of multiopcodes.
			if ((operand.GetType() != thisOperand.GetType()) &&
				(!opCode.IsMulti || operand.GetType().IsPrimitive != thisOperand.GetType().IsPrimitive))
				return false;
			
			switch (operand) {
			case ParameterDefinition parameter:
				return (parameter.Index == ((ParameterDefinition) thisOperand).Index);
			case VariableDefinition variable:
				return (variable.Index == ((VariableDefinition) thisOperand).Index);
			case MemberReference member:
				return (member.FullName == ((MemberReference) thisOperand).FullName);
			case CallSite callSite:
				return (callSite.FullName == ((CallSite) thisOperand).FullName);
			case Instruction instr:
				return instr.EqualsInstruction((Instruction) thisOperand);
			case Instruction[] instrArray:
				Instruction[] thisInstrArray = (Instruction[]) thisOperand;
				if (instrArray.Length != thisInstrArray.Length)
					return false;
				for (int i = 0; i < instrArray.Length; i++) {
					if (!instrArray[i].EqualsInstruction(thisInstrArray[i]))
						return false;
				}
				return true;
			}

			// Primitive & string values
			return operand.Equals(thisOperand);
		}

		#endregion

		/*#region IsParameter/Variable

		/// <summary>
		/// Checks if the opcode refers to a parameter definition.
		/// </summary>
		/// <param name="opCode">The opcode to check.</param>
		/// <returns>True if the opcode refers to a parameter definition.</returns>
		private static bool IsParameterOpCode(AnyOpCode opCode) {
			if (opCode.IsMulti) {
				MultiOpCodes multi = opCode.MultiOpCode;
				return (multi == MultiOpCodes.Ldarg  ||
						multi == MultiOpCodes.Ldarga ||
						multi == MultiOpCodes.Starg);
			}
			return opCode.OpCode.OperandType == OperandType.InlineArg;
		}
		/// <summary>
		/// Checks if the opcode refers to a variable definition.
		/// </summary>
		/// <param name="opCode">The opcode to check.</param>
		/// <returns>True if the opcode refers to a variable definition.</returns>
		private static bool IsVariableOpCode(AnyOpCode opCode) {
			if (opCode.IsMulti) {
				MultiOpCodes multi = opCode.MultiOpCode;
				return (multi == MultiOpCodes.Ldloc  ||
						multi == MultiOpCodes.Ldloca ||
						multi == MultiOpCodes.Stloc);
			}
			return opCode.OpCode.OperandType == OperandType.InlineVar;
		}

		#endregion*/
	}
}
