using System;
using System.Linq;
using Mono.Cecil;

namespace TriggersTools.ILPatching {
	partial class IL {
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
			throw new ArgumentException($"Failed to get type and member name of '{fullName}'!");
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
				throw new ArgumentException($"Failed to locate '{moduleName}' module definition!");

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
				throw new ArgumentException($"Failed to locate '{typeName}' type definition!");

			return typeDefinition;
		}
		/// <summary>
		/// Gets the definition of a modfule's type. Only use this instead of GetTypeDefinition() when the type is not within the Terraria module (eg. an XNA type).
		/// </summary>
		public static TypeReference GetTypeReference(ModuleDefinition moduleDefinition, string fullTypeName) {
			if (!moduleDefinition.TryGetTypeReference(fullTypeName, out TypeReference reference))
				throw new ArgumentException($"Failed to locate '{fullTypeName}' type reference!");

			return reference;
		}
		/// <summary>
		/// Gets the definition of a type's field.
		/// </summary>
		public static FieldDefinition GetFieldDefinition(TypeDefinition typeDefinition, string fieldName) {
			FieldDefinition fieldDefinition = typeDefinition.Fields
				.FirstOrDefault(f => f.Name == fieldName);

			if (fieldDefinition == null)
				throw new ArgumentException($"Failed to locate '{typeDefinition.FullName}.{fieldName}' field definition!");

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
				throw new ArgumentException($"Failed to locate '{typeDefinition.FullName}.{propName}' property definition!");

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
				throw new ArgumentException($"Failed to locate '{typeDefinition.FullName}.{methodName}()' method definition!");

			return methodDefinition;
		}

		#endregion
	}
}
