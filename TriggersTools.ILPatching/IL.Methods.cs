using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching {
	partial class IL {
		#region InstructionCount

		/// <summary>
		/// Gets the number of instructions in the method.
		/// </summary>
		/// <param name="method">The method definition to get the instruction count from.</param>
		/// <returns>The instruction count.</returns>
		public static int InstructionCount(MethodDefinition method) {
			return method.Body.Instructions.Count;
		}

		#endregion

		#region Insert

		/// <summary>
		/// Prepents instructions to the beginning of the specified method.
		/// </summary>
		/// <param name="method">The method definition to prepend instructions to.</param>
		/// <param name="instructions">The instructions to prepend.</param>
		/// <returns>The index of the next instruction after the last prepended instruction.</returns>
		public static int MethodPrepend(MethodDefinition method, params Instruction[] instructions) {
			return MethodInsert(method, 0, instructions);
		}
		/// <summary>
		/// Appends instructions to the end of the specified method.
		/// </summary>
		/// <param name="method">The method definition to append instructions to.</param>
		/// <param name="instructions">The instructions to append.</param>
		/// <returns>The index of the next instruction after the last appended instruction.</returns>
		public static int MethodAppend(MethodDefinition method, params Instruction[] instructions) {
			return MethodInsert(method, InstructionCount(method), instructions);
		}
		/// <summary>
		/// Inserts instructions into the specified method.
		/// </summary>
		/// <param name="method">The method definition to insert instructions into.</param>
		/// <param name="index">The index to insert the instruction at.</param>
		/// <param name="instructions">The instructions to insert.</param>
		/// <returns>The index of the next instruction after the last inserted instruction.</returns>
		public static int MethodInsert(MethodDefinition method, int index, params Instruction[] instructions) {
			foreach (var instr in instructions) {
				method.Body.Instructions.Insert(index, instr);
				index++;
			}
			return index;
		}

		#endregion

		#region Replace

		/// <summary>
		/// Replaces all of the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace the with new instructions.</param>
		/// <param name="instructions">The instructions to replace with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodOverwrite(MethodDefinition method, params Instruction[] instructions) {
			method.Body.Instructions.Clear();
			foreach (var instr in instructions) {
				method.Body.Instructions.Add(instr);
			}
			return instructions.Length;
		}
		/// <summary>
		/// Replaces all of the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace the with new instructions.</param>
		/// <param name="clearLocals">True if all local variables should be cleared.</param>
		/// <param name="instructions">The instructions to replace with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodOverwrite(MethodDefinition method, bool clearLocals,
			params Instruction[] instructions)
		{
			method.Body.Instructions.Clear();
			if (clearLocals)
				method.Body.Variables.Clear();
			foreach (var instr in instructions) {
				method.Body.Instructions.Add(instr);
			}
			return instructions.Length;
		}
		/// <summary>
		/// Replaces a single instruction of the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace an instruction in.</param>
		/// <param name="index">The index of the only instruction to replace.</param>
		/// <param name="instructions">The instructions to replace the single instruction with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodReplaceSingle(MethodDefinition method, int index,
			params Instruction[] instructions)
		{
			method.Body.Instructions.RemoveAt(index);
			foreach (var instr in instructions) {
				method.Body.Instructions.Insert(index, instr);
				index++;
			}
			return index;
		}
		/// <summary>
		/// Replaces a range of instructions in the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace instructions in.</param>
		/// <param name="start">The index of the first instruction to replace.</param>
		/// <param name="end">The index after the last instruction to replace.</param>
		/// <param name="instructions">The instructions to replace the range of instructions with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodReplaceRange(MethodDefinition method, int start, int end,
			params Instruction[] instructions)
		{
			for (int i = start; i < end; i++) {
				method.Body.Instructions.RemoveAt(start);
			}
			foreach (var instr in instructions) {
				method.Body.Instructions.Insert(start, instr);
				start++;
			}
			return start;
		}
		/// <summary>
		/// Replaces the start of the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace instructions in.</param>
		/// <param name="end">
		/// The index after the last instruction to replace from the start of the method.
		/// </param>
		/// <param name="instructions">The instructions to replace the start of instructions with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodReplaceStart(MethodDefinition method, int end,
			params Instruction[] instructions)
		{
			return MethodReplaceRange(method, 0, end, instructions);
		}
		/**<summary></summary>*/
		/// <summary>
		/// Replaces the end of the specified method.
		/// </summary>
		/// <param name="method">The method definition to replace instructions in.</param>
		/// <param name="start">
		/// The index of the first instruction to replace until the end of the method.
		/// </param>
		/// <param name="instructions">The instructions to replace the end of instructions with.</param>
		/// <returns>The index of the next instruction after the last new instruction.</returns>
		public static int MethodReplaceEnd(MethodDefinition method, int start,
			params Instruction[] instructions)
		{
			return MethodReplaceRange(method, start, InstructionCount(method), instructions);
		}

		#endregion

		#region Remove
		
		/// <summary>
		/// Clears the specified method of all instructions.
		/// </summary>
		/// <param name="method">The method definition to clear instructions from.</param>
		public static void MethodClear(MethodDefinition method) {
			method.Body.Instructions.Clear();
		}
		/// <summary>
		/// Clears the specified method of all instructions.
		/// </summary>
		/// <param name="method">The method definition to clear instructions from.</param>
		public static void MethodClear(MethodDefinition method, bool clearLocals) {
			method.Body.Instructions.Clear();
			if (clearLocals)
				method.Body.Variables.Clear();
		}
		/// <summary>
		/// Removes a single instruction in the specified method.
		/// </summary>
		/// <param name="method">The method definition to remove an instruction from.</param>
		/// <param name="index">The index of the instruction to remove.</param>
		public static void MethodRemoveSingle(MethodDefinition method, int index) {
			method.Body.Instructions.RemoveAt(index);
		}
		/// <summary>
		/// Removes a range of instructions in the specified method.
		/// </summary>
		/// <param name="method">The method definition to remove instructions from.</param>
		/// <param name="start">The index of the first instruction to remove.</param>
		/// <param name="end">The index after the last instruction to remove.</param>
		public static void MethodRemoveRange(MethodDefinition method, int start, int end) {
			for (int i = start; i < end; i++) {
				method.Body.Instructions.RemoveAt(start);
			}
		}
		/// <summary>
		/// Removes the start of the specified method.
		/// </summary>
		/// <param name="method">The method definition to remove instructions from.</param>
		/// <param name="end">
		/// The index after the last instruction to remove from the start of the method.
		/// </param>
		public static void MethodRemoveStart(MethodDefinition method, int end) {
			MethodRemoveRange(method, 0, end);
		}
		/// <summary>
		/// Removes the end of the specified method.
		/// </summary>
		/// <param name="method">The method definition to remove instructions from.</param>
		/// <param name="start">
		/// The index of the first instruction to remove until the end of the method.
		/// </param>
		public static void MethodRemoveEnd(MethodDefinition method, int start) {
			MethodRemoveRange(method, start, InstructionCount(method));
		}

		#endregion
	}
}
