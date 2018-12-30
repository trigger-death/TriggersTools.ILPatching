using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching {
	partial class IL {

		#region ThrowIfInvalidOperandType

		/// <summary>
		/// Throws an exception if the operand is not a valid operand type.
		/// </summary>
		/// <param name="operand">The operand of unknown type.</param>
		/// 
		/// <exception cref="ArgumentException">
		/// <paramref name="operand"/> is not a valid operend type.
		/// </exception>
		internal static void ThrowIfInvalidOperandType(object operand) {
			if (!IsValidOperandType(operand))
				throw new ArgumentException($"{operand.GetType().Name} is not a valid operand type!");
		}
		/// <summary>
		/// Throws an exception if the operand is not a valid operand type.
		/// </summary>
		/// <param name="operand">The operand of unknown type.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="type"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="type"/> is not a valid operend type.
		/// </exception>
		internal static void ThrowIfInvalidOperandType(Type type) {
			if (!IsValidOperandType(type))
				throw new ArgumentException($"{type.Name} is not a valid operand type!");
		}

		#endregion

		#region ThrowIfInvalidCaptureName

		private const string ValidateCapturePattern = @"^[A-Za-z_]\w*$";
		private static readonly Regex ValidateCaptureRegex = new Regex(ValidateCapturePattern);

		/// <summary>
		/// Throws an <see cref="ArgumentException"/> if the capture name is invalid.
		/// </summary>
		/// <param name="captureName">The capture name to test.</param>
		/// 
		/// <exception cref="ArgumentNullException">
		/// <paramref name="captureName"/> is null and <paramref name="allowNull"/> is false.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="captureName"/> is not a valid capture name.
		/// </exception>
		internal static void ThrowIfInvalidCaptureName(string captureName, bool allowNull) {
			if (captureName == null && !allowNull) {
				throw new ArgumentNullException(nameof(captureName));
			}
			if (captureName != null && !ValidateCaptureRegex.IsMatch(captureName)) {
				throw new ArgumentException($"Capture name \"{captureName}\" can only have alphanumeric characters, " +
										   $"must not start with a digit, and cannot be empty!");
			}
		}

		#endregion
	}
}
