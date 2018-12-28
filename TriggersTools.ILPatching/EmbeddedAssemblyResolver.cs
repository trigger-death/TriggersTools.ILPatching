using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace TriggersTools.ILPatching {
	/// <summary>
	/// Resolves assemblies for the patcher by checking for embedded resources.
	/// </summary>
	public class EmbeddedAssemblyResolver : BaseAssemblyResolver {
		#region Constants

		/// <summary>
		/// The default settings for whether executables are included in the search.
		/// </summary>
		public const bool IncludeExesDefault = false;

		#endregion

		#region Fields

		/// <summary>
		/// The collection of assembly resource names.
		/// </summary>
		private Dictionary<string, string> assemblyResources;
		/// <summary>
		/// Gets the list of assembly used to search for embedded assemblies.
		/// </summary>
		public IReadOnlyList<Assembly> Assemblies { get; }
		/// <summary>
		/// Gets if executables should be looked for in embedded resources.
		/// </summary>
		public bool IncludeExes { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the default embedded assembly resolver, using the entry assembly.
		/// </summary>
		public EmbeddedAssemblyResolver() : this(new[] { Assembly.GetEntryAssembly() }) { }
		/// <summary>
		/// Constructs the default embedded assembly resolver, using the entry assembly.
		/// </summary>
		/// <param name="includeExes">True if executables should be looked for in embedded resources.</param>
		public EmbeddedAssemblyResolver(bool includeExes)
			: this(includeExes, new[] { Assembly.GetEntryAssembly() })
		{
		}

		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assembly.
		/// </summary>
		/// <param name="assembly">The single assembly to use for embedded resolution.</param>
		public EmbeddedAssemblyResolver(Assembly assembly) : this(new[] { assembly }) { }
		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assembly.
		/// </summary>
		/// <param name="assembly">The single assembly to use for embedded resolution.</param>
		/// <param name="includeExes">True if executables should be looked for in embedded resources.</param>
		public EmbeddedAssemblyResolver(bool includeExes, Assembly assembly)
			: this(includeExes, new[] { assembly })
		{
		}


		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assemblies.
		/// </summary>
		/// <param name="assemblies">The assemblies to use for embedded resolution.</param>
		public EmbeddedAssemblyResolver(params Assembly[] assemblies)
			: this((IEnumerable<Assembly>) assemblies)
		{
		}
		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assemblies.
		/// </summary>
		/// <param name="assemblies">The assemblies to use for embedded resolution.</param>
		/// <param name="includeExes">True if executables should be looked for in embedded resources.</param>
		public EmbeddedAssemblyResolver(bool includeExes, params Assembly[] assemblies)
			: this(includeExes, (IEnumerable<Assembly>) assemblies)
		{
		}

		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assemblies.
		/// </summary>
		/// <param name="assemblies">The assemblies to use for embedded resolution.</param>
		public EmbeddedAssemblyResolver(IEnumerable<Assembly> assemblies)
			: this(IncludeExesDefault, assemblies)
		{
		}
		/// <summary>
		/// Constructs the embedded assembly resolver using the specified assemblies.
		/// </summary>
		/// <param name="assemblies">The assemblies to use for embedded resolution.</param>
		/// <param name="includeExes">True if executables should be looked for in embedded resources.</param>
		public EmbeddedAssemblyResolver(bool includeExes, IEnumerable<Assembly> assemblies) {
			if (!assemblies.Any())
				throw new ArgumentException("At least one assembly must be specified for the embedded assembly resolver!");
			assemblyResources = new Dictionary<string, string>();
			Assemblies = assemblies.ToImmutableArray();
			IncludeExes = includeExes;
		}

		#endregion

		#region Resolving

		/// <summary>
		/// Resolves the assembly name.
		/// </summary>
		/// <param name="name">The name of the assembly reference to resolve.</param>
		/// <returns>The resolved assembly definition.</returns>
		public override AssemblyDefinition Resolve(AssemblyNameReference name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			foreach (Assembly assembly in Assemblies) {
				// Attempt to read a predefined resource assembly
				if (assemblyResources.ContainsKey(name.Name)) {
					using (Stream stream = assembly.GetManifestResourceStream(assemblyResources[name.Name])) {
						if (stream != null)
							return ModuleDefinition.ReadModule(stream).Assembly;
					}
				}
				// Attempt to read a dll resource assembly
				using (Stream stream = assembly.GetManifestResourceStream(name.Name + ".dll")) {
					if (stream != null)
						return ModuleDefinition.ReadModule(stream).Assembly;
				}
				// Attempt to read an exe resource assembly
				using (Stream stream = assembly.GetManifestResourceStream(name.Name + ".exe")) {
					if (stream != null)
						return ModuleDefinition.ReadModule(stream).Assembly;
				}
			}

			return base.Resolve(name);
		}
		/// <summary>
		/// Adds an assembly name as a resource name to be resolved later.
		/// </summary>
		/// <param name="assemblyName">The name of the assembly to resolve.</param>
		/// <param name="resourceName">The name of the resource containing the assembly.</param>
		public void AddEmbeddedAssembly(string assemblyName, string resourceName) {
			assemblyResources.Add(assemblyName, resourceName);
		}

		#endregion
	}
}
