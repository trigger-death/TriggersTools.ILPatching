using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILRegexRunner {
		#region Match

		/// <summary>
		/// Call this to begin an initial match state, or to continue an existing one.
		/// </summary>
		/// <param name="state">The match state with the opcheck to test.</param>
		/// <returns>True if the match was successful, otherwise false.</returns>
		private bool Match(MatchState state) {
			if (state.IsGroup) {
				if (state.IsGreedy != options.HasFlag(ILRegexOptions.SwapGreedy))
					return QuantifyGreedyGroup(state);
				else
					return QuantifyLazyGroup(state);
			}
			else {
				if (state.IsGreedy != options.HasFlag(ILRegexOptions.SwapGreedy))
					return QuantifyGreedyOpCheck(state);
				else
					return QuantifyLazyOpCheck(state);
			}
		}

		#endregion

		#region Quantify Lazy

		/// <summary>
		/// Performs lazy match quantification for a group.
		/// </summary>
		/// <param name="state">The match state for the group.</param>
		/// <returns>True if the quantification was successful.</returns>
		private bool QuantifyLazyGroup(MatchState state) {
			if (!state.Started) {
				// Quantify until minimum, backtrack when match fails
				while (state.Count < state.Min) {
#if DEBUG
					if (!DebugMatchGroup(state)) {
#else
					if (!MatchGroup(state)) {
#endif
						if (!QuantifyGroupBacktrack(state))
							return false; // Can't backtrack
					}
				}
				state.Started = true;
			}
			else {
				// At this moment, CurrentMax.Count >= MIN
				do {
#if DEBUG
					bool isMax = state.Count == state.Max;
					if (state.Count == state.Max || !DebugMatchGroup(state)) {
						if (isMax)
							OutputDebugStatus(state, false, "Group", false, "MAX REACHED");
#else
					if (state.Count == state.Max || !MatchGroup(state)) {
#endif
						if (!QuantifyGroupBacktrack(state))
							return false; // Can't backtrack
					}
				} while (state.Count < state.Min);
			}
			return true;
		}
		/// <summary>
		/// Performs lazy match quantification for a literal opcheck.
		/// </summary>
		/// <param name="state">The match state for the literal opcheck.</param>
		/// <returns>True if the quantification was successful.</returns>
		private bool QuantifyLazyOpCheck(MatchState state) {
			if (!state.Started) {
				while (state.Count < state.Min) {
#if DEBUG
					if (!DebugMatchOpCheck(state))
#else
					if (!MatchOpCheck(state))
#endif
						return false; // Minimum never reached
				}
				state.Started = true;
			}
			else {
#if DEBUG
				bool isMax = state.Count == state.Max;
				if (state.Count == state.Max || !DebugMatchOpCheck(state)) {
					if (isMax)
						OutputDebugStatus(state, false, "OpCheck", false, "MAX REACHED");
#else
				if (state.Count == state.Max || !MatchOpCheck(state)) {
#endif

					return false; // Can't quantify anymore
				}
			}
			return true;
		}

		#endregion

		#region Quantify Greedy

		/// <summary>
		/// Performs greedy match quantification for a group.
		/// </summary>
		/// <param name="state">The match state for the group.</param>
		/// <returns>True if the quantification was successful.</returns>
		private bool QuantifyGreedyGroup(MatchState state) {
			if (!state.Started) {
				QuantifyGreedyGroupMaximum(state);

				if (!QuantifyGreedyGroupMinimum(state))
					return false;
				state.Started = true;
			}
			else {
				// At this moment, CurrentMax.Count >= MIN
				if (state.Count == 0)
					return false; // Can't backtrack
#if DEBUG
				if (!DebugContinueGroup(state)) {
#else
				if (!ContinueGroup(state)) {
#endif
					state.Matches.Pop();

					// If we've popped out of the minimum range
					if (!QuantifyGreedyGroupMinimum(state))
						return false; // Can't backtrack
									  // Try to continue next match again
				}
				else {
					// Continue our greedy nature and match as many more as possible
					QuantifyGreedyGroupMaximum(state);
				}
			}
			return true;
		}
		/// <summary>
		/// Performs greedy match quantification for a literal opcheck.
		/// </summary>
		/// <param name="state">The match state for the literal opcheck.</param>
		/// <returns>True if the quantification was successful.</returns>
		private bool QuantifyGreedyOpCheck(MatchState state) {
			if (!state.Started) {
				while (state.Count < state.Max) {
#if DEBUG
					if (!DebugMatchOpCheck(state))
#else
					if (!MatchOpCheck(state))
#endif
						break; // We've found as many as we can for now
				}

				if (state.Count < state.Min)
					return false; // Minimum never reached
				state.Started = true;
			}
			else {
				// At this moment, CurrentMax.Count >= MIN
				if (state.Count == state.Min)
					return false; // Can't backtrack
				state.Matches.Pop();
			}
			return true;
		}

		#endregion

		#region Quantify Helpers

		/// <summary>
		/// Backtracks the group state until a match can be continued.
		/// </summary>
		/// <param name="state">The match state for the group.</param>
		/// <returns>
		/// True if a match was able to be continued, false if the group can no longer backtrack.
		/// </returns>
		private bool QuantifyGroupBacktrack(MatchState state) {
			if (state.Count == 0)
				return false; // Can't backtrack

#if DEBUG
			while (!DebugContinueGroup(state)) {
#else
			while (!ContinueGroup(state)) {
#endif
				state.Matches.Pop();
				if (state.Count == 0)
					return false; // Can't backtrack
			}
			return true;
		}
		/// <summary>
		/// Tries to backtrack the group state and at least matches the minimum number of times
		/// </summary>
		/// <param name="state">The match state for the group.</param>
		/// <returns>
		/// True if the group was able to be matched the minimum number of times, otherwise false.
		/// </returns>
		private bool QuantifyGreedyGroupMinimum(MatchState state) {
			while (state.Count < state.Quantifier.Min) {
				if (!QuantifyGroupBacktrack(state))
					return false; // Can't backtrack
								  // Continue our greedy nature and match as many more as possible
				QuantifyGreedyGroupMaximum(state);
			}
			return true;
		}
		/// <summary>
		/// Matches the group state as many times as possible.
		/// </summary>
		/// <param name="state">The match state for the group.</param>
		private void QuantifyGreedyGroupMaximum(MatchState state) {
			while (state.Count < state.Max) {
#if DEBUG
				if (!DebugMatchGroup(state))
#else
				if (!MatchGroup(state))
#endif
					break; // We've found as many as we can for now
			}
		}

		#endregion

		#region Debug Helpers
#if DEBUG
		private bool DebugMatchGroup(MatchState state) {
			bool success = MatchGroup(state);
			OutputDebugStatus(state, success, "Group", false);
			return success;
		}
		private bool DebugContinueGroup(MatchState state) {
			bool success = ContinueGroup(state);
			OutputDebugStatus(state, success, "Group", true);
			return success;
		}
		private bool DebugMatchOpCheck(MatchState state) {
			bool success = MatchOpCheck(state);
			OutputDebugStatus(state, success, "OpCheck", false);
			return success;
		}

		private void OutputDebugStatus(MatchState state, bool success, string type, bool isContinue, string result = null) {
			if (!CanOutputDebug)
				return;
			Debug.WriteLine($"{DebugSpacing}{type.PadRight(7)} " + // Type
				$"<{state.OpCheckIndex.ToString().PadLeft(2)}>" + // OpCheck Index
				$"[{(state.Count + (success || isContinue ? 0 : 1))}] " + // Match Count
				$"{(isContinue ? "CONTINUE" : "MATCH").PadRight(8)} " + // Action
				(result ?? (success ? string.Empty : "FAILED"))); // Result
		}

		private string DebugSpacing => new string('.', (level - 1) * 1);

		private bool CanOutputDebug => ILRegex.VerboseDebug && (!ILRegex.FirstLevelDebugOnly || level == 1);
#endif
		#endregion
	}
}
