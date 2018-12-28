using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// Opcodes with multiple instructions that perform the same function but with different parameters.
	/// </summary>
	public enum MultiOpCodes {
		/// <summary>This value is not used.</summary>
		Invalid = -1,
		/// <summary>No opcode.</summary>
		Any,

		/// <summary>Ldarg_0-3, Ldarg_S, Ldarg.</summary>
		Ldarg,
		/// <summary>Ldarga_S, Ldarga.</summary>
		Ldarga,
		/// <summary>Starg_S, Starg.</summary>
		Starg,

		/// <summary>Ldloc_0-3, Ldloc_S, Ldloc.</summary>
		Ldloc,
		/// <summary>Ldloca_S, Ldloca.</summary>
		Ldloca,
		/// <summary>Stloc_0-3, Stloc_S, Stloc.</summary>
		Stloc,

		/// <summary>Ldc_I4_0-8, Ldc_I4_M1, Ldc_I4_S, Ldc_I4.</summary>
		Ldc_I4,

		/// <summary>Beq_S, Beq.</summary>
		Beq,
		/// <summary>Bne_Un_S, Bne_Un.</summary>
		Bne_Un,

		/// <summary>Bge_S, Bge.</summary>
		Bge,
		/// <summary>Bge_Un_S, Bge_Un.</summary>
		Bge_Un,
		/// <summary>Bgt_S, Bgt.</summary>
		Bgt,
		/// <summary>Bgt_Un_S, Bgt_Un.</summary>
		Bgt_Un,

		/// <summary>Ble_S, Ble.</summary>
		Ble,
		/// <summary>Ble_Un_S, Ble_Un.</summary>
		Ble_Un,
		/// <summary>Blt_S, Blt.</summary>
		Blt,
		/// <summary>Blt_Un_S, Blt_Un.</summary>
		Blt_Un,

		/// <summary>Br_S, Br.</summary>
		Br,
		/// <summary>Brtrue_S, Brtrue.</summary>
		Brtrue,
		/// <summary>Brfalse_S, Brfalse.</summary>
		Brfalse,
		/// <summary>Brinst_S, Brinst.</summary>
		Brinst = Brtrue,
		/// <summary>Brnull_S, Brnull.</summary>
		Brnull = Brfalse,
		/// <summary>Brzero_S, Brzero.</summary>
		Brzero = Brfalse,

		/// <summary>Leave_S, Leave.</summary>
		Leave,
	}

	/// <summary>
	/// Extension methods for <see cref="MultiOpCodes"/>.
	/// </summary>
	public static class MultiOpCodesExtensions {
		/// <summary>
		/// Compares the opcode to the multiple instruction opcode.
		/// </summary>
		/// <param name="multiOpCode">The multiple-possibility opcode.</param>
		/// <param name="opCode">The normal opcode to compare against.</param>
		/// <returns>True if the opcodes match.</returns>
		public static bool EqualsOpCode(this MultiOpCodes multiOpCode, OpCode opCode) {
			Code code = opCode.Code;
			switch (multiOpCode) {
			case MultiOpCodes.Any:
				return true;

			// Parameter
			case MultiOpCodes.Ldarg:
				return (code >= Code.Ldarg_0  && code <= Code.Ldarg_3) ||
						code == Code.Ldarg_S  || code == Code.Ldarg;
			case MultiOpCodes.Ldarga:
				return (code == Code.Ldarga_S || code == Code.Ldarga);
			case MultiOpCodes.Starg:
				return (code == Code.Starg_S  || code == Code.Starg);
			
			// Local Variable
			case MultiOpCodes.Ldloc:
				return (code >= Code.Ldloc_0   && code <= Code.Ldloc_3) ||
						code == Code.Ldloc_S   || code == Code.Ldloc;
			case MultiOpCodes.Ldloca:
				return (code == Code.Ldloca_S  || code == Code.Ldloca);
			case MultiOpCodes.Stloc:
				return (code >= Code.Stloc_0   && code <= Code.Stloc_3) ||
						code == Code.Stloc_S   || code == Code.Stloc;
			
			// Integer
			case MultiOpCodes.Ldc_I4:
				return (code >= Code.Ldc_I4_M1 && code <= Code.Ldc_I4);

			// Branching:

			// Equality
			case MultiOpCodes.Beq:
				return (code == Code.Beq_S     || code == Code.Beq);
			case MultiOpCodes.Bne_Un:
				return (code == Code.Bne_Un_S  || code == Code.Bne_Un);

			// Greater Than
			case MultiOpCodes.Bge:
				return (code == Code.Bge_S     || code == Code.Bge);
			case MultiOpCodes.Bge_Un:
				return (code == Code.Bge_Un_S  || code == Code.Bge_Un);
			case MultiOpCodes.Bgt:
				return (code == Code.Bgt_S     || code == Code.Bgt);
			case MultiOpCodes.Bgt_Un:
				return (code == Code.Bgt_Un_S  || code == Code.Bgt_Un);

			// Less Than
			case MultiOpCodes.Ble:
				return (code == Code.Ble_S     || code == Code.Ble);
			case MultiOpCodes.Ble_Un:
				return (code == Code.Ble_Un_S  || code == Code.Ble_Un);
			case MultiOpCodes.Blt:
				return (code == Code.Blt_S     || code == Code.Blt);
			case MultiOpCodes.Blt_Un:
				return (code == Code.Blt_Un_S  || code == Code.Blt_Un);

			// Other Branching
			case MultiOpCodes.Br:
				return (code == Code.Br_S      || code == Code.Br);
			case MultiOpCodes.Brtrue:
				return (code == Code.Brtrue_S  || code == Code.Brtrue);
			case MultiOpCodes.Brfalse:
				return (code == Code.Brfalse_S || code == Code.Brfalse);
				
			// Other
			case MultiOpCodes.Leave:
				return (code == Code.Leave_S   || code == Code.Leave);

			default:
				return false;
			}
		}
	}
}
