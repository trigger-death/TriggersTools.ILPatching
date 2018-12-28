using System.Text.RegularExpressions;

namespace TriggersTools.ILPatching.Internal {
	/// <summary>
	/// Static methods to extract format flags for ToString methods and remove them from the string.
	/// </summary>
	internal static class FormatUtils {
		#region HasToken

		/// <summary>
		/// Gets if the format string has the specified token, and removes it if it does.
		/// </summary>
		/// <param name="token">The token to check for.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <returns>True if the token was found.</returns>
		public static bool HasToken(string token, ref string s) {
			int index = s.IndexOf(token);
			if (index == -1)
				return false;
			s = s.Substring(0, index) + s.Substring(index + token.Length);
			return true;
		}

		#endregion

		#region HasPattern

		/// <summary>
		/// Gets if the format string has the specified regex pattern, and removes the match if it does.
		/// </summary>
		/// <param name="pattern">The regex pattern to check for.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <returns>True if the regex pattern was found.</returns>
		public static bool HasPattern(string pattern, ref string s) {
			return HasPattern(pattern, ref s, out Match _);
		}
		/// <summary>
		/// Gets if the format string has the specified regex pattern, and removes the match if it does.
		/// </summary>
		/// <param name="pattern">The regex pattern to check for.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <param name="result">The value of the regex pattern match.</param>
		/// <returns>True if the regex pattern was found.</returns>
		public static bool HasPattern(string pattern, ref string s, out string result) {
			if (HasPattern(pattern, ref s, out Match match))
				result = match.Value;
			else
				result = null;
			return match.Success;
		}
		/// <summary>
		/// Gets if the format string has the specified regex pattern, and removes the match if it does.
		/// </summary>
		/// <param name="pattern">The regex pattern to check for.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <param name="match">The regex pattern match.</param>
		/// <returns>True if the regex pattern was found.</returns>
		public static bool HasPattern(string pattern, ref string s, out Match match) {
			match = Regex.Match(s, pattern);
			if (!match.Success) {
				return false;
			}
			s = s.Substring(0, match.Index) + s.Substring(match.Index + match.Length);
			return true;
		}

		#endregion

		#region HasMatch

		/// <summary>
		/// Gets if the format string has the specified regex, and removes the match if it does.
		/// </summary>
		/// <param name="regex">The regex to check with.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <returns>True if the regex found a match.</returns>
		public static bool HasMatch(Regex regex, ref string s) {
			return HasMatch(regex, ref s, out Match _);
		}
		/// <summary>
		/// Gets if the format string has the specified regex, and removes the match if it does.
		/// </summary>
		/// <param name="regex">The regex to check with.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <param name="result">The value of the regex match.</param>
		/// <returns>True if the regex found a match.</returns>
		public static bool HasMatch(Regex regex, ref string s, out string result) {
			if (HasMatch(regex, ref s, out Match match))
				result = match.Value;
			else
				result = null;
			return match.Success;
		}
		/// <summary>
		/// Gets if the format string has the specified regex, and removes the match if it does.
		/// </summary>
		/// <param name="regex">The regex to check with.</param>
		/// <param name="s">The format string to check in and modify.</param>
		/// <param name="match">The regex match.</param>
		/// <returns>True if the regex found a match.</returns>
		public static bool HasMatch(Regex regex, ref string s, out Match match) {
			match = regex.Match(s);
			if (!match.Success) {
				return false;
			}
			s = s.Substring(0, match.Index) + s.Substring(match.Index + match.Length);
			return true;
		}

		#endregion
	}
}
