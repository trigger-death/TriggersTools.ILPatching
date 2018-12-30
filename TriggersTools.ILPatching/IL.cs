using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TriggersTools.ILPatching.RegularExpressions;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// The main class for IL operations to modify methods or create instructions.
	/// </summary>
	/// <remarks>
	/// https://github.com/dougbenham/TerrariaPatcher/blob/master/IL.cs
	/// </remarks>
	public static partial class IL {
		#region AddStaticConstructor

		/// <summary>
		/// Adds a static constructor to the type definition if one exists, otherwise it returns the existing
		/// constructor.
		/// </summary>
		/// <param name="type">The type definition to add the static constructor to.</param>
		/// <returns>The method definition for the static constructor.</returns>
		public static MethodDefinition AddStaticConstructor(TypeDefinition type) {
			MethodDefinition staticConstructor = GetMethodDefinition(type, ".cctor");
			if (staticConstructor == null) {
				const MethodAttributes attributes =
					MethodAttributes.Private|
					MethodAttributes.HideBySig |
					MethodAttributes.SpecialName |
					MethodAttributes.RTSpecialName |
					MethodAttributes.Static;

				staticConstructor = new MethodDefinition(".cctor", attributes, type.Module.TypeSystem.Void);
				type.Methods.Add(staticConstructor);
			}
			return staticConstructor;
		}

		#endregion

		#region Add/ModifyStaticField


		/// <summary>
		/// Adds a new static field to a type.
		/// </summary>
		/// <param name="classType">The type definition for the class to add the static field to.</param>
		/// <param name="fieldName">The name of the field to create.</param>
		/// <param name="fieldType">The type definition of the field.</param>
		/// <param name="value">The initial value of the field.</param>
		/// <returns>The definition for the newly created field.</returns>
		public static FieldDefinition AddStaticField(TypeDefinition classType, string fieldName,
			TypeReference fieldType, object value = null)
		{
			const FieldAttributes attributes = FieldAttributes.Static | FieldAttributes.Public;
			FieldDefinition field = new FieldDefinition(fieldName, attributes, fieldType);
			classType.Fields.Add(field);
			if (value != null) {
				Instruction loadInstr = loadInstr = CreateLoadInstruction(value);
				Instruction storeInstr = Instruction.Create(OpCodes.Stsfld, field);
				AddStaticFieldSetter(field, value, true);
			}
			return field;
		}

		/// <summary>
		/// Modifies a static field by appending the store instructions to the end of the static constructor.
		/// </summary>
		/// <param name="classType">The type definition for the class owning the static field.</param>
		/// <param name="fieldName">The name of the static field to modify.</param>
		/// <param name="newValue">The new value of the static field.</param>
		public static void ModifyStaticFieldAppend(TypeDefinition classType, string fieldName,
			object newValue)
		{
			FieldDefinition field = GetFieldDefinition(classType, fieldName);
			ModifyStaticFieldAppend(field, newValue);
		}
		/// <summary>
		/// Modifies a static field by appending the store instructions to the end of the static constructor.
		/// </summary>
		/// <param name="field">The static field to modify.</param>
		/// <param name="newValue">The new value of the static field.</param>
		public static void ModifyStaticFieldAppend(FieldDefinition field, object newValue) {
			AddStaticFieldSetter(field, newValue, false);
		}
		/// <summary>
		/// Modifies a static field by modifying the last store instruction in the static constructor.
		/// </summary>
		/// <param name="classType">The type definition for the class owning the static field.</param>
		/// <param name="fieldName">The name of the static field to modify.</param>
		/// <param name="newValue">The new value of the static field.</param>
		public static void ModifyStaticFieldInline(TypeDefinition classType, string fieldName,
			object newValue)
		{
			FieldDefinition field = GetFieldDefinition(classType, fieldName);
			ModifyStaticFieldInline(field, newValue);
		}
		/// <summary>
		/// Modifies a static field by modifying the last store instruction in the static constructor.
		/// </summary>
		/// <param name="field">The static field to modify.</param>
		/// <param name="newValue">The new value of the static field.</param>
		public static void ModifyStaticFieldInline(FieldDefinition field, object newValue) {
			if (!field.Attributes.HasFlag(FieldAttributes.Static))
				throw new Exception($"Field {field.Name} is not a static field!");

			MethodDefinition staticConstructor = AddStaticConstructor(field.DeclaringType);
			Instruction newLoadInstr = CreateLoadInstruction(newValue);

			// Find the last time the field is stored to.
			for (int i = staticConstructor.Body.Instructions.Count - 1; i >= 0; i--) {
				Instruction storeInstr = staticConstructor.Body.Instructions[i];
				if (storeInstr.OpCode == OpCodes.Stsfld && i > 0) {
					FieldDefinition operandField = (FieldDefinition) storeInstr.Operand;
					Instruction loadInstr = staticConstructor.Body.Instructions[i - 1];
					if (operandField.FullName == field.FullName && loadInstr.OpCode == newLoadInstr.OpCode) {
						// This is for sure, the instruction that loads the value to assign to the field.
						staticConstructor.Body.Instructions[i - 1] = loadInstr;
					}
				}
			}
			throw new Exception($"Failed to locate point in static construtor where static field {field.Name} is assigned!");
		}

		/// <summary>
		/// Appends or preprends static field setter instructions 
		/// </summary>
		/// <param name="field">The static field definition to assign to.</param>
		/// <param name="value">The value to assign to the static field.</param>
		/// <param name="prepend">True if the assignment should be prepended instead of appended.</param>
		private static void AddStaticFieldSetter(FieldDefinition field, object value, bool prepend) {
			MethodDefinition staticConstructor = AddStaticConstructor(field.DeclaringType);
			
			if (value == null) {
				if (field.FieldType.IsValueType)
					throw new Exception($"Cannot assign null to value type {field.FieldType.Name}!");
			}
			else if (field.FieldType.FullName != value.GetType().FullName)
				throw new Exception($"Field type {field.FieldType.Name} and value type {value.GetType().Name} are not the same!");

			Instruction loadInstr = CreateLoadInstruction(value);
			Instruction storeInstr = Instruction.Create(OpCodes.Stsfld, field);

			// First load the value onto the stack then store the value into the field.
			Instruction[] instructions = new[] {
				loadInstr,
				storeInstr,
			};
			if (prepend)
				MethodPrepend(staticConstructor, instructions);
			else
				MethodAppend(staticConstructor, instructions);
		}

		#endregion

		#region CreateLoadInstruction

		/// <summary>
		/// Creates a load instruction for a primitive type or field.
		/// </summary>
		/// <param name="value">The value to assign with the load instruction.</param>
		/// <returns>The created instruction.</returns>
		/// 
		/// <exception cref="ArgumentException">
		/// <paramref name="value"/> has an unsupported type for this method.
		/// </exception>
		public static Instruction CreateLoadInstruction(object value) {
			if (value == null)
				return Instruction.Create(OpCodes.Ldnull);

			switch (value) {
			case bool casted:
				return Instruction.Create(casted ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

			case string casted:
				return Instruction.Create(OpCodes.Ldstr, casted);

			// Floating
			case float casted:
				return Instruction.Create(OpCodes.Ldc_R4, casted);
			case double casted:
				return Instruction.Create(OpCodes.Ldc_R8, casted);

			// Byte cast
			case sbyte casted:
				return Instruction.Create(OpCodes.Ldc_I4_S, casted);
			case byte casted:
				return Instruction.Create(OpCodes.Ldc_I4_S, unchecked((sbyte) casted));

			// Int cast
			case char casted:
				return Instruction.Create(OpCodes.Ldc_I4, casted);
			case short casted:
				return Instruction.Create(OpCodes.Ldc_I4, casted);
			case ushort casted:
				return Instruction.Create(OpCodes.Ldc_I4, casted);
			case int casted:
				return Instruction.Create(OpCodes.Ldc_I4, casted);
			case uint casted:
				return Instruction.Create(OpCodes.Ldc_I4, unchecked((int) casted));

			// Long cast
			case long casted:
				return Instruction.Create(OpCodes.Ldc_I8, casted);
			case ulong casted:
				return Instruction.Create(OpCodes.Ldc_I8, unchecked((long) casted));

			default:
				throw new ArgumentException($"Unsupported basic field type {value.GetType().Name}!");
			}
		}

		#endregion

		#region MakeTypePublic

		/// <summary>
		/// Makes the type definition and every member inside of it public
		/// </summary>
		/// <param name="typeDefinition">The type definition to make everything public in.</param>
		/// <param name="includeNestedTypes">Recursively makes all nested types public.</param>
		public static void MakeTypePublic(TypeDefinition typeDefinition, bool includeNestedTypes = true) {
			// Make nested types public
			if (includeNestedTypes) {
				foreach (var definition in typeDefinition.NestedTypes) {
					if (definition.FullName != typeDefinition.FullName) // avoid infinite recursion
						MakeTypePublic(definition, true);
				}
			}

			// Make type public
			// Set this if we are working with a nested type definition, (unrelated to includeNestedTypes)
			if (typeDefinition.IsNested)
				typeDefinition.IsNestedPublic = true;
			else
				typeDefinition.IsPublic = true;

			// Make members public
			foreach (var field in typeDefinition.Fields)
				field.IsPublic = true;
			foreach (var prop in typeDefinition.Properties) {
				// Properties themselves cannot be public, but their getters and setters can be.
				if (prop.GetMethod != null)
					prop.GetMethod.IsPublic = true;
				if (prop.SetMethod != null)
					prop.SetMethod.IsPublic = true;
			}
			foreach (var method in typeDefinition.Methods) {
				if (!method.IsSpecialName)
					method.IsPublic = true;
			}
		}

		#endregion

		#region CreateInstruction

		/// <summary>
		/// Creates an <see cref="Instruction"/> using the operand with the specified name in the
		/// <see cref="ILOperandDictionary"/>.
		/// </summary>
		/// <param name="opcode">The opcode of the instruction.</param>
		/// <param name="ops">The operand dictionary to get the operand from.</param>
		/// <param name="name">The name of the operand to get.</param>
		/// <returns>The created instruction.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="ops"/> or <paramref name="name"/> is null.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// <paramref name="name"/> does not exist in <paramref name="ops"/>.
		/// </exception>
		public static Instruction CreateInstruction(OpCode opcode, ILOperandDictionary ops, string name) {
			if (ops == null)
				throw new ArgumentNullException(nameof(ops));
			return CreateInstruction(opcode, ops[name]);
		}
		/// <summary>
		/// Creates an <see cref="Instruction"/> using an operand of unknown type.
		/// </summary>
		/// <param name="opcode">The opcode of the instruction.</param>
		/// <param name="operand">The operand of unknown type.</param>
		/// <returns>The created instruction.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="ops"/> or <paramref name="name"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="operand"/> is not a valid operand type.
		/// </exception>
		public static Instruction CreateInstruction(OpCode opcode, object operand) {
			switch (operand) {
			// No operand
			case null:
				return Instruction.Create(opcode);

			// Basic operands
			case int value:
				return Instruction.Create(opcode, value);
			case long value:
				return Instruction.Create(opcode, value);
			case byte value:
				return Instruction.Create(opcode, value);
			case sbyte value:
				return Instruction.Create(opcode, value);
			case float value:
				return Instruction.Create(opcode, value);
			case double value:
				return Instruction.Create(opcode, value);
			case string value:
				return Instruction.Create(opcode, value);

			// Ref-a-Def-CallSite operands
			case ParameterDefinition parameter:
				return Instruction.Create(opcode, parameter);
			case VariableDefinition variable:
				return Instruction.Create(opcode, variable);
			case FieldReference field:
				return Instruction.Create(opcode, field);
			case MethodReference method:
				return Instruction.Create(opcode, method);
			case TypeReference type:
				return Instruction.Create(opcode, type);
			case CallSite site:
				return Instruction.Create(opcode, site);

			// Nested instruction operands
			case Instruction target:
				return Instruction.Create(opcode, target);
			case Instruction[] targets:
				return Instruction.Create(opcode, targets);
			
			default:
				throw new ArgumentException($"Invalid operand type '{operand.GetType().Name}'!", nameof(operand));
			}
		}

		#endregion

		#region IsValidOperandType

		public static bool IsValidOperandType(object operand) {
			switch (operand) {
			// No operand
			case null:

			// Basic operands
			case int _:
			case long _:
			case byte _:
			case sbyte _:
			case float _:
			case double _:
			case string _:

			// Ref-a-Def-CallSite operands
			case ParameterDefinition _:
			case VariableDefinition _:
			case FieldReference _:
			case MethodReference _:
			case TypeReference _:
			case CallSite _:

			// Nested instruction operands
			case Instruction _:
			case Instruction[] _:
				return true;
				
			default:
				return false;
			}
		}
		public static bool IsValidOperandType(Type type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			switch (Type.GetTypeCode(type)) {
			case TypeCode.DBNull:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.String:
				return true;
			default:
				return (type == typeof(ParameterDefinition) || type == typeof(TypeDefinition) ||
						type == typeof(VariableDefinition)  || type == typeof(CallSite)       ||
						type == typeof(FieldDefinition)     || type == typeof(Instruction)    ||
						type == typeof(MethodDefinition)    || type == typeof(Instruction[]));
			}
		}

		#endregion
		
		#region EqualsOperand

		/// <summary>
		/// Checks if the two specified operands are equal to each other.
		/// </summary>
		/// <param name="operandA">The first operand to compare.</param>
		/// <param name="operandB">The second operand to compare.</param>
		/// <returns>True if the operands are equal to each other.</returns>
		public static bool EqualsOperand(object operandA, object operandB) {
			if (operandA == null || operandB == null)
				return (operandA == null && operandB == null);
			
			if (operandA.GetType() != operandB.GetType())
				return false;

			switch (operandA) {
			case ParameterDefinition parameterA:
				return (parameterA.Index == ((ParameterDefinition) operandB).Index);
			case VariableDefinition variableA:
				return (variableA.Index == ((VariableDefinition) operandB).Index);
			case MemberReference memberA:
				CallSite memberB = (CallSite) operandB;
				return (memberA.FullName        == memberB.FullName &&
						memberA.Module.FileName == memberB.Module.FileName);
			case CallSite callSiteA:
				CallSite callSiteB = (CallSite) operandB;
				return (callSiteA.FullName        == callSiteB.FullName &&
						callSiteA.Module.FileName == callSiteB.Module.FileName);
			case Instruction instrA:
				return (instrA == ((Instruction) operandB));
			case Instruction[] instrArrayA:
				Instruction[] instrArrayB = (Instruction[]) operandB;
				if (instrArrayA.Length != instrArrayB.Length)
					return false;
				for (int i = 0; i < instrArrayA.Length; i++) {
					if (instrArrayA[i] != instrArrayB[i])
						return false;
				}
				return true;
			}

			// Primitive & string values
			return operandA.Equals(operandB);
		}

		#endregion
	}
}
