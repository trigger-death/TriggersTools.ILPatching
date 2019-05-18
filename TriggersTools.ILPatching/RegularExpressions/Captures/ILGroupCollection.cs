using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// A collection of matched IL instruction groups with optional names.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ILGroupCollection : IReadOnlyList<ILGroup>  {
		#region Fields
		
		/// <summary>
		/// The array of captured instruction groups.
		/// </summary>
		private readonly ILGroup[] groups;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs the collection from an array.
		/// </summary>
		/// <param name="groups">The array of groups to use.</param>
		internal ILGroupCollection(ILGroup[] groups) {
			this.groups = groups;
		}

		#endregion

		#region IReadOnlyList Implementation

		/// <summary>
		/// Gets the group at the specified index.
		/// </summary>
		/// <param name="index">The index of the matched group.</param>
		/// <returns>The matched group if it exists, otherwise an unsuccessful group.</returns>
		public ILGroup this[int index] {
			get => (index >= 0 && index < Count ? groups[index] : ILGroup.EmptyGroup);
		}
		/// <summary>
		/// Gets the group with the specified name.
		/// </summary>
		/// <param name="name">The name of the matched group.</param>
		/// <returns>The matched group if it exists, otherwise an unsuccessful group.</returns>
		public ILGroup this[string name] {
			get {
				if (name == null)
					throw new ArgumentNullException(nameof(name));
				else if (int.TryParse(name, out int index))
					return this[index];
				return Array.Find(groups, g => g.Name == name) ?? ILGroup.EmptyGroup;
			}
		}
		/// <summary>
		/// Gets the number of matched groups.
		/// </summary>
		public int Count => groups.Length;
		/// <summary>
		/// Gets the enumerator for the IL group collection.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<ILGroup> GetEnumerator() => ((IEnumerable<ILGroup>) groups).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

		#region DebuggerDisplay

		private string DebuggerDisplay => $"Count = {Count}";

		#endregion
	}
}
