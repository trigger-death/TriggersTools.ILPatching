using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace TriggersTools.ILPatching {
	partial class IL {
		#region LargeAddressAware

		/// <summary>
		/// Checks if the specified assembly file is already Large Address Aware.
		/// </summary>
		/// <param name="file">The path to the assembly file.</param>
		/// <returns>True if the assembly file is Large Address Aware</returns>
		/// 
		/// <exception cref="InvalidOperationException">
		/// Could not locate the assembly's MZ or PE header.
		/// </exception>
		public static bool IsLargeAddressAware(string file) {
			using (var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
				const short IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

				BinaryReader reader = new BinaryReader(stream);

				if (stream.Length < 0x3C)
					throw new InvalidOperationException("Stream is not large enough to have an MZ or PE header!");

				if (reader.ReadInt16() != 0x5A4D) // No MZ Header
					throw new InvalidOperationException("No MZ Header!");

				stream.Position = 0x3C;
				int peloc = reader.ReadInt32();   // Get the PE header location.

				stream.Position = peloc;
				if (reader.ReadInt32() != 0x4550) // No PE header
					throw new InvalidOperationException("No PE Header!");

				stream.Position += 0x12;

				long position = stream.Position;
				short flags = reader.ReadInt16();
				return (flags & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE;
			}
		}
		/// <summary>
		/// Patches the executable to allow more memory usage.<para/>
		/// This is required after Mono.cecil writes to the assembly.
		/// </summary>
		/// <param name="file">The path to the assembly file to make Large Address Aware.</param>
		/// <returns>True if the assembly was made Large Address Aware, false if it already was.</returns>
		/// 
		/// <exception cref="InvalidOperationException">
		/// Could not locate the assembly's MZ or PE header.
		/// </exception>
		public static bool MakeLargeAddressAware(string file) {
			using (var stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)) {
				const short IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

				BinaryReader reader = new BinaryReader(stream);
				BinaryWriter writer = new BinaryWriter(stream);

				if (stream.Length < 0x3C)
					throw new InvalidOperationException("Stream is not large enough to have an MZ or PE header!");

				if (reader.ReadInt16() != 0x5A4D) // No MZ Header
					throw new InvalidOperationException("No MZ Header!");

				stream.Position = 0x3C;
				int peloc = reader.ReadInt32();   // Get the PE header location.

				stream.Position = peloc;
				if (reader.ReadInt32() != 0x4550) // No PE header
					throw new InvalidOperationException("No PE Header!");

				stream.Position += 0x12;

				long position = stream.Position;
				short flags = reader.ReadInt16();
				bool isLAA = (flags & IMAGE_FILE_LARGE_ADDRESS_AWARE) == IMAGE_FILE_LARGE_ADDRESS_AWARE;
				if (isLAA)                        // Already Large Address Aware
					return false;

				flags |= IMAGE_FILE_LARGE_ADDRESS_AWARE;

				stream.Position = position;
				//writer.Seek((int) position, SeekOrigin.Begin);
				writer.Write(flags);
				writer.Flush();
				return true;
			}
		}

		#endregion

		#region GetAssemblyVersion

		/// <summary>
		/// Gets the version of the assembly.
		/// </summary>
		/// <param name="path">The path of the assembly file.</param>
		/// <returns>The version of the assembly.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="path"/> is null.
		/// </exception>
		public static Version GetAssemblyVersion(string path) {
			using (var assembly = AssemblyDefinition.ReadAssembly(path))
				return assembly.Name.Version;
		}

		#endregion
	}
}
