using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// An opcode that can either represent a single standard <see cref="Mono.Cecil.Cil.OpCode"/> or
	/// <see cref="MultiOpCodes"/>.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public struct AnyOpCode {
		#region Constants

		/// <summary>
		/// A constant opcode that represents any and all opcodes.
		/// </summary>
		public static readonly AnyOpCode Any = new AnyOpCode(MultiOpCodes.Any);

		#endregion

		#region Static Fields

		/// <summary>
		/// The dictionary mapping all opcode names to values, because there is no option for parsing.
		/// </summary>
		private static readonly Dictionary<string, OpCode> opCodeMap =
			new Dictionary<string, OpCode>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Gets the length of the longest opcode name.
		/// </summary>
		public static int LongestOpCodeNameLength { get; private set; }

		#endregion

		#region Static Initializer

		/// <summary>
		/// Initializes the opcode map.
		/// </summary>
		static AnyOpCode() {
			foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)) {
				if (field.FieldType == typeof(OpCode))
					AddOpCode(field.Name, (OpCode) field.GetValue(null));
			}
			// Aliases
			AddOpCode("Brinst", OpCodes.Brtrue);
			AddOpCode("Brinst_S", OpCodes.Brtrue_S);
			AddOpCode("brnull", OpCodes.Brfalse);
			AddOpCode("brnull_S", OpCodes.Brfalse_S);
			AddOpCode("brzero", OpCodes.Brfalse);
			AddOpCode("brzero_S", OpCodes.Brfalse_S);
			AddOpCode("Ldelem_U8", OpCodes.Ldelem_I8);
			AddOpCode("Ldind_U8", OpCodes.Ldind_I8);
			// Ending in .'s
			AddOpCode("Constrained_", OpCodes.Constrained);
			AddOpCode("No_", OpCodes.No);
			AddOpCode("Readonly_", OpCodes.Readonly);
			AddOpCode("Tail_", OpCodes.Tail);
			AddOpCode("Volatile_", OpCodes.Volatile);
		}
		private static void AddOpCode(string name, OpCode opcode) {
			opCodeMap.Add(name, opcode);
			LongestOpCodeNameLength = Math.Max(LongestOpCodeNameLength, name.Length);
		}

		#endregion

		#region Static ListOpCodes

		/// <summary>
		/// Gets the names of all normal opcodes.
		/// </summary>
		/// <returns>An array of all normal opcode names.</returns>
		public static string[] GetOpCodeNames() {
			return opCodeMap.Keys.Select(n => n.Replace('_', '.').ToLower()).ToArray();
		}
		/// <summary>
		/// Gets the names of all multi opcodes.
		/// </summary>
		/// <returns>An array of all multi opcode names.</returns>
		public static string[] GetMultiOpCodeNames() {
			return Enum.GetNames(typeof(MultiOpCodes)).Select(n => n.Replace('_', '.').ToLower()).ToArray();
		}
		
		#endregion

		#region Fields

		/// <summary>
		/// A constant OpCode.
		/// </summary>
		public OpCode OpCode { get; }
		/// <summary>
		/// An OpCode that represents many possibilities of the same action.
		/// </summary>
		public MultiOpCodes MultiOpCode { get; }

		#endregion

		#region Properties

		/// <summary>
		/// Gets if this opcode is using <see cref="MultiOpCode"/>.
		/// </summary>
		public bool IsMulti => MultiOpCode != MultiOpCodes.Invalid;
		/// <summary>
		/// Gets if this opcode represents any and all opcodes.
		/// </summary>
		public bool IsAny => MultiOpCode == MultiOpCodes.Any;
		/// <summary>
		/// Gets if the opcode refers to a parameter definition.
		/// </summary>
		public bool IsParameterOpCode {
			get {
				if (IsMulti) {
					return (MultiOpCode == MultiOpCodes.Ldarg  ||
							MultiOpCode == MultiOpCodes.Ldarga ||
							MultiOpCode == MultiOpCodes.Starg);
				}
				return  OpCode.OperandType == OperandType.InlineArg ||
						OpCode.OperandType == OperandType.ShortInlineArg;
			}
		}
		/// <summary>
		/// Gets if the opcode refers to a variable definition.
		/// </summary>
		public bool IsVariableOpCode {
			get {
				if (IsMulti) {
					return (MultiOpCode == MultiOpCodes.Ldloc  ||
							MultiOpCode == MultiOpCodes.Ldloca ||
							MultiOpCode == MultiOpCodes.Stloc);
				}
				return  OpCode.OperandType == OperandType.InlineVar ||
						OpCode.OperandType == OperandType.ShortInlineVar;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the comparable opcode from a standard opcode.
		/// </summary>
		/// <param name="opCode">The standard opcode to use.</param>
		public AnyOpCode(OpCode opCode) {
			OpCode = opCode;
			MultiOpCode = MultiOpCodes.Invalid;
		}
		/// <summary>
		/// Constructs the comparable opcode from a multiple-instruction opcode.
		/// </summary>
		/// <param name="opCode">The multiple-instruction opcode to use.</param>
		public AnyOpCode(MultiOpCodes locOpCode) {
			OpCode = OpCodes.Nop;
			MultiOpCode = locOpCode;
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Gets the string representation of the opcode.
		/// </summary>
		/// <returns>The string representation of the opcode.</returns>
		public override string ToString() {
			if (IsMulti) return $"%{MultiOpCode.ToString().Replace('_', '.').ToLower()}";
			return OpCode.ToString();
		}
		private string DebuggerDisplay => ToString();
		/// <summary>
		/// Checks if this opcode and another object are equal.
		/// </summary>
		/// <param name="obj">The object to compare with.</param>
		/// <returns>True if the objects are equal.</returns>
		public override bool Equals(object obj) {
			if (obj is OpCode otherOpCode) {
				if (IsMulti)
					return MultiOpCode.EqualsOpCode(otherOpCode);
				return OpCode == otherOpCode;
			}
			else if (obj is MultiOpCodes otherMultiOpCode) {
				if (IsMulti)
					return MultiOpCode == otherMultiOpCode;
				return otherMultiOpCode.EqualsOpCode(OpCode);
			}
			else if (obj is AnyOpCode otherAnyOpCode) {
				return this == otherAnyOpCode;
			}
			return base.Equals(obj);
		}
		/// <summary>
		/// Gets the hashcode representation of the opcode.
		/// </summary>
		/// <returns>The computed hash code.</returns>
		public override int GetHashCode() {
			if (IsMulti) // Add arbitrary extra bit to differentiate between standard OpCodes
				return int.MinValue | (int) MultiOpCode;
			return (int) OpCode.Code;
		}

		#endregion

		#region Parsing
		
		/// <summary>
		/// Parses the opcode from the string. <see cref="MultiOpCodes"/> begin with '$'.
		/// </summary>
		/// <param name="s">The string representation of the opcode.</param>
		/// <returns>The parsed opcode.</returns>
		/// 
		/// <remarks>
		/// Parsing is case-insensitive and opcodes can either use '.'s or '_'s.
		/// </remarks>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="s"/> is null.
		/// </exception>
		/// <exception cref="FormatException">
		/// The string does not represent a valid opcode.
		/// </exception>
		public static AnyOpCode Parse(string s) {
			string original = s;
			s = s.ToLower();
			s = s.Replace('.', '_');
			if (s.StartsWith("%")) {
				if (Enum.TryParse(s.Substring(1), true, out MultiOpCodes multiOpCode))
					return multiOpCode;
				throw new FormatException($"Invalid MultiOpCode \"{original}\"!");
			}
			if (opCodeMap.TryGetValue(s, out OpCode opCode))
				return opCode;
			throw new FormatException($"Invalid OpCode \"{original}\"!");
		}
		
		#endregion

		#region Operators

		public static bool operator ==(AnyOpCode a, AnyOpCode b) {
			if (a.IsMulti) {
				if (b.IsMulti)
					return a.MultiOpCode == b.MultiOpCode;
				return a.MultiOpCode.EqualsOpCode(b.OpCode);
			}
			if (b.IsMulti)
				return b.MultiOpCode.EqualsOpCode(a.OpCode);
			return a.OpCode == b.OpCode;
		}
		public static bool operator ==(AnyOpCode a, MultiOpCodes b) {
			if (a.IsMulti)
				return a.MultiOpCode == b;
			return b.EqualsOpCode(a.OpCode);
		}
		public static bool operator ==(MultiOpCodes a, AnyOpCode b) {
			if (b.IsMulti)
				return a == b.MultiOpCode;
			return a.EqualsOpCode(b.OpCode);
		}
		public static bool operator ==(AnyOpCode a, OpCode b) {
			if (a.IsMulti)
				return a.MultiOpCode.EqualsOpCode(b);
			return a.OpCode == b;
		}
		public static bool operator ==(OpCode a, AnyOpCode b) {
			if (b.IsMulti)
				return b.MultiOpCode.EqualsOpCode(a);
			return a == b.OpCode;
		}

		public static bool operator !=(AnyOpCode a, AnyOpCode b) => !(a == b);
		public static bool operator !=(AnyOpCode a, MultiOpCodes b) => !(a == b);
		public static bool operator !=(MultiOpCodes a, AnyOpCode b) => !(a == b);
		public static bool operator !=(AnyOpCode a, OpCode b) => !(a == b);
		public static bool operator !=(OpCode a, AnyOpCode b) => !(a == b);

		#endregion

		#region Casting

		public static implicit operator AnyOpCode(OpCode opCode) => new AnyOpCode(opCode);
		public static implicit operator AnyOpCode(MultiOpCodes locOpCode) => new AnyOpCode(locOpCode);

		#endregion
	}
}
