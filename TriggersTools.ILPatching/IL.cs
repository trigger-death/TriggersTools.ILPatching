using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// The main helper class for scanning and modifying assemblies.
	/// </summary>
	/// <remarks>
	/// https://github.com/dougbenham/TerrariaPatcher/blob/master/IL.cs
	/// </remarks>
	public static class IL {
		#region Methods
		//--------------------------------
		#region Getters
			
		/// <summary>
		/// Gets the number of instructions in the method.
		/// </summary>
		/// <param name="method">The method definition to get the instruction count from.</param>
		/// <returns>The instruction count.</returns>
		public static int InstructionCount(MethodDefinition method) {
			return method.Body.Instructions.Count;
		}

		#endregion
		//--------------------------------
		#region Prepend

		/// <summary>
		/// Prepents instructions to the beginning of the specified method.
		/// </summary>
		/// <param name="method">The method definition to prepend instructions to.</param>
		/// <param name="instructions">The instructions to prepend.</param>
		/// <returns>The index of the next instruction after the last prepended instruction.</returns>
		public static int MethodPrepend(MethodDefinition method, params Instruction[] instructions) {
			return MethodInsert(method, 0, instructions);
		}

		#endregion
		//--------------------------------
		#region Append

		/// <summary>
		/// Appends instructions to the end of the specified method.
		/// </summary>
		/// <param name="method">The method definition to append instructions to.</param>
		/// <param name="instructions">The instructions to append.</param>
		/// <returns>The index of the next instruction after the last appended instruction.</returns>
		public static int MethodAppend(MethodDefinition method, params Instruction[] instructions) {
			return MethodInsert(method, InstructionCount(method), instructions);
		}

		#endregion
		//--------------------------------
		#region Insert
		
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
		//--------------------------------
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
		//--------------------------------
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
		//--------------------------------
		#endregion
		
		#region Definitions

		/// <summary>
		/// Gets the last two '.' separated names in the full name.<para/>
		/// Usually used to get the type and member name.
		/// </summary>
		/// <param name="fullName">The full name to get the last two names from.</param>
		private static string[] GetTypeAndMemberName(string fullName) {
			int memberStart = fullName.LastIndexOf('.');
			if (memberStart != -1) {
				int typeStart = fullName.LastIndexOf('.', memberStart - 1);
				if (typeStart != -1) {
					return new[] {
						fullName.Substring(typeStart, memberStart - typeStart), // type
						fullName.Substring(memberStart + 1), // member
					};
				}
			}
			throw new Exception($"Failed to get type and member name of '{fullName}'!");
		}

		/// <summary>
		/// Gets the definition of an assembly's module.
		/// </summary>
		/// <param name="asmDefinition">The assembly definition containing the module.</param>
		/// <param name="moduleName">The name of the module. (no .dll)</param>
		public static ModuleDefinition GetModuleDefinition(AssemblyDefinition asmDefinition,
			string moduleName)
		{
			ModuleDefinition moduleDefinition = asmDefinition.Modules
				.FirstOrDefault(p => p.FileName == moduleName);

			if (moduleDefinition == null)
				throw new Exception($"Failed to locate '{moduleName}' module definition!");

			return moduleDefinition;
		}
		/// <summary>
		/// Gets the definition of a module's type.
		/// </summary>
		/// <param name="moduleDefinition">The module definition containing the type.</param>
		public static TypeDefinition GetTypeDefinition(ModuleDefinition moduleDefinition, string typeName,
			bool fullName = false)
		{
			TypeDefinition typeDefinition;
			if (fullName) {
				typeDefinition = moduleDefinition.Types
					.FirstOrDefault(t => t.FullName == typeName);
			}
			else {
				typeDefinition = moduleDefinition.Types
					.FirstOrDefault(t => t.Name == typeName);
			}
			
			if (typeDefinition == null)
				throw new Exception($"Failed to locate '{typeName}' type definition!");

			return typeDefinition;
		}
		/// <summary>
		/// Gets the definition of a modfule's type. Only use this instead of GetTypeDefinition() when the type is not within the Terraria module (eg. an XNA type).
		/// </summary>
		public static TypeReference GetTypeReference(ModuleDefinition moduleDefinition, string fullTypeName) {
			if (!moduleDefinition.TryGetTypeReference(fullTypeName, out TypeReference reference))
				throw new Exception($"Failed to locate '{fullTypeName}' type reference!");

			return reference;
		}
		/// <summary>
		/// Gets the definition of a type's field.
		/// </summary>
		public static FieldDefinition GetFieldDefinition(TypeDefinition typeDefinition, string fieldName) {
			FieldDefinition fieldDefinition = typeDefinition.Fields
				.FirstOrDefault(f => f.Name == fieldName);

			if (fieldDefinition == null)
				throw new Exception($"Failed to locate '{typeDefinition.FullName}.{fieldName}' field definition!");

			return fieldDefinition;
		}
		/// <summary>
		/// Gets the definition of a type's field.
		/// </summary>
		public static FieldDefinition GetFieldDefinition(ModuleDefinition moduleDefinition, string fullName) {
			string[] typeAndMember = GetTypeAndMemberName(fullName);
			TypeDefinition typeDefinition = GetTypeDefinition(moduleDefinition, typeAndMember[0]);
			return GetFieldDefinition(typeDefinition, typeAndMember[1]);
		}
		/// <summary>
		/// Gets the definition of a type's property.
		/// </summary>
		public static PropertyDefinition GetPropertyDefinition(TypeDefinition typeDefinition, string propName) {
			PropertyDefinition propDefinition = typeDefinition.Properties
				.FirstOrDefault(p => p.Name == propName);

			if (propDefinition == null)
				throw new Exception($"Failed to locate '{typeDefinition.FullName}.{propName}' property definition!");

			return propDefinition;
		}
		/// <summary>
		/// Gets the definition of a type's property.
		/// </summary>
		public static PropertyDefinition GetPropertyDefinition(ModuleDefinition moduleDefinition, string fullName) {
			string[] typeAndMember = GetTypeAndMemberName(fullName);
			TypeDefinition typeDefinition = GetTypeDefinition(moduleDefinition, typeAndMember[0]);
			return GetPropertyDefinition(typeDefinition, typeAndMember[1]);
		}
		/// <summary>
		/// Gets the definition of a type's method.
		/// </summary>
		/// <param name="typeDefinition">The type to get the method from.</param>
		/// <param name="methodName">The name of the method to look for.</param>
		/// <param name="parameterCount">The optional required number of parameters.</param>
		/// <param name="isStatic">The optional requirement of being static.</param>
		/// <returns>The located method definition.</returns>
		public static MethodDefinition GetMethodDefinition(TypeDefinition typeDefinition, string methodName,
			int? parameterCount = null, bool? isStatic = null)
		{
			MethodDefinition methodDefinition;
			if (parameterCount.HasValue) {
				if (isStatic.HasValue) {
					methodDefinition = typeDefinition.Methods
						.FirstOrDefault(m => m.Name == methodName &&
										m.IsStatic == isStatic.Value &&
										m.Parameters.Count == parameterCount.Value);
				}
				else {
					methodDefinition = typeDefinition.Methods
						.FirstOrDefault(m => m.Name == methodName &&
										m.Parameters.Count == parameterCount.Value);
				}
			}
			else {
				if (isStatic.HasValue) {
					methodDefinition = typeDefinition.Methods
						.FirstOrDefault(m => m.Name == methodName &&
										m.IsStatic == isStatic.Value);
				}
				else {
					methodDefinition = typeDefinition.Methods
						.FirstOrDefault(m => m.Name == methodName);
				}
			}

			if (methodDefinition == null)
				throw new Exception($"Failed to locate '{typeDefinition.FullName}.{methodName}()' method definition!");

			return methodDefinition;
		}

		#endregion

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
				throw new Exception($"Unsupported basic field type {value.GetType().Name}!");
			}
		}

		#endregion

		#region MakeLargeAddressAware

		/// <summary>
		/// Patches the executable to allow more memory usage.<para/>
		/// This is required after Mono.cecil writes to the assembly.
		/// </summary>
		public static void MakeLargeAddressAware(string file) {
			using (var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
				const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

				BinaryReader reader = new BinaryReader(stream);
				BinaryWriter writer = new BinaryWriter(stream);

				if (reader.ReadInt16() != 0x5A4D)       //No MZ Header
					return;

				reader.BaseStream.Position = 0x3C;
				int peloc = reader.ReadInt32();         //Get the PE header location.

				reader.BaseStream.Position = peloc;
				if (reader.ReadInt32() != 0x4550)       //No PE header
					return;

				reader.BaseStream.Position += 0x12;

				long position = reader.BaseStream.Position;
				short flags = reader.ReadInt16();
				bool isLAA = (flags & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE;
				if (isLAA)                          //Already Large Address Aware
					return;

				flags |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

				writer.Seek((int) position, SeekOrigin.Begin);
				writer.Write(flags);
				writer.Flush();
			}
		}

		#endregion

		#region GetAssemblyVersion

		/// <summary>
		/// Gets the version of the assembly.
		/// </summary>
		/// <param name="path">The path of the assembly file.</param>
		/// <returns>The version of the assembly.</returns>
		public static Version GetAssemblyVersion(string path) {
			using (var assembly = AssemblyDefinition.ReadAssembly(path))
				return assembly.Name.Version;
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
	}
}
