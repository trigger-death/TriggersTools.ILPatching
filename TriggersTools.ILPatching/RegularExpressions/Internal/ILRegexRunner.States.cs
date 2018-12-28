using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILRegexRunner {
		/// <summary>
		/// Data used for backtracking instruction matches.
		/// </summary>
		private class MatchState {
			#region Fields

			/// <summary>
			/// Gets the op check that was called.
			/// </summary>
			public ILCheck OpCheck { get; set; }

			/// <summary>
			/// Gets if the match state has already started and may possibly be continued.
			/// </summary>
			public bool Started { get; set; }
			/// <summary>
			/// The previous match state before this one.
			/// </summary>
			public MatchState PreviousState { get; set; }
			/// <summary>
			/// Gets the quantifier matches stack.
			/// </summary>
			public Stack<QuantifierMatch> Matches { get; } = new Stack<QuantifierMatch>();

			#endregion

			#region Properties

			// OpCheck:
			/// <summary>
			/// Gets the index of the opcheck in the regex.
			/// </summary>
			public int OpCheckIndex => OpCheck.OpCheckIndex;
			/// <summary>
			/// Gets if the opcheck is a group opcheck.
			/// </summary>
			public bool IsGroup => OpCheck.Code == OpChecks.GroupStart;

			// Quantifier:
			/// <summary>
			/// Gets the optional quantifier after the instruction check.
			/// </summary>
			public ILQuantifier Quantifier => OpCheck.Quantifier;
			/// <summary>
			/// Gets if the opcheck's quantifier is just one. (No quantifier)
			/// </summary>
			public bool IsOne => OpCheck.Quantifier.IsOne;
			/// <summary>
			/// Gets the minimum count of the opcheck's quantifier.
			/// </summary>
			public int Min => OpCheck.Quantifier.Min;
			/// <summary>
			/// Gets the maximum count of the opcheck's quantifier.
			/// </summary>
			public int Max => OpCheck.Quantifier.Max;
			/// <summary>
			/// Gets if the opcheck's quantifier is greedy.
			/// </summary>
			public bool IsGreedy => OpCheck.Quantifier.IsGreedy;

			// Instruction Position:
			/// <summary>
			/// Gets the starting instruction position before the match.
			/// </summary>
			public int Start => PreviousState.End;
			/// <summary>
			/// Gets the end instruction position after the match.
			/// </summary>
			public int End => LastMatch.End;
			
			// Matches:
			/// <summary>
			/// Gets the number of times a quantifier has been matched with.
			/// </summary>
			public int Count => Matches.Count;
			/*/// <summary>
			/// Gets the top match in the state chain.<para/>
			/// This will return the previous state's match if this state has no matches.
			/// </summary>
			public QuantifierMatch TopMatch {
				get {
					if (Matches.Count != 0) {
						//foreach (QuantifierMatch match in Matches) {
						//	if (match.Stack != null && match.Stack.Count != 0)
						//		return match.Stack.Peek().TopMatch;
						//}
						QuantifierMatch match = Matches.Peek();
						if (match.Stack != null && match.Stack.Count != 0)
							return match.Stack.Peek().TopMatch;
						return match;
					}
					return PreviousState.TopMatch;
				}
			}*/
			/// <summary>
			/// Gets the last current-level match in the state chain.<para/>
			/// This will return the previous state's match if this state has no matches.
			/// </summary>
			public QuantifierMatch LastMatch {
				get {
					if (Matches.Count != 0)
						return Matches.Peek();
					return PreviousState.LastMatch;
				}
			}
			/*/// <summary>
			/// Gets the last match in the state chain.<para/>
			/// This will return the previous state's match if this state has no matches.
			/// </summary>
			public QuantifierMatch LastMatch {
				get {
					if (Matches.Count != 0) {
						QuantifierMatch match = Matches.Peek();
						if (match.Stack != null && match.Stack.Count != 0)
							return match.Stack.Peek().LastMatch;
						return match;
					}
					return PreviousState.LastMatch;
				}
			}*/
			/// <summary>
			/// Gets the second last match in the state chain.<para/>
			/// This will return the previous state's match if this state has no matches.
			/// </summary>
			public QuantifierMatch SecondLastMatch {
				get {
					if (Matches.Count > 1)
						return Matches.ElementAt(1);
					else if (Matches.Count == 1)
						return PreviousState.LastMatch;
					else
						return PreviousState.SecondLastMatch;
				}
			}
			/// <summary>
			/// Gets the last state in the state chain.
			/// </summary>
			public MatchState LastState {
				get {
					if (Matches.Count != 0 && IsGroup) {
						foreach (QuantifierMatch match in Matches) {
							if (match.Stack.Count != 0)
								return match.Stack.Peek();
						}
						/*QuantifierMatch match = Matches.Peek();
						if (match.Stack != null && match.Stack.Count != 0)
							return match.Stack.Peek().TopState;*/
					}
					// Groups aren't a normal state, and thus cannot be relied on as being the last state.
					if (IsGroup)
						return PreviousState;
					return this;
				}
			}
			/*/// <summary>
			/// Gets the top state in the state chain.
			/// </summary>
			public MatchState TopState {
				get {
					if (Matches.Count != 0 && IsGroup) {
						foreach (QuantifierMatch match in Matches) {
							if (match.Stack.Count != 0)
								return match.Stack.Peek().TopState;
						}
						//QuantifierMatch match = Matches.Peek();
						//if (match.Stack != null && match.Stack.Count != 0)
						//	return match.Stack.Peek().TopState;
					}
					// Groups aren't a normal state, and thus cannot be relied on as being the last state.
					if (IsGroup)
						return PreviousState;
					return this;
				}
			}*/
			/// <summary>
			/// Gets the index of the last opcheck.
			/// </summary>
			public int LastOpCheckIndex {
				get {
					if (Matches.Count != 0 && IsGroup) {
						QuantifierMatch match = Matches.Peek();
						if (match.Stack.Count != 0) {
							var state = match.Stack.Peek();
							if (state.IsGroup)
								return state.OpCheck.GroupOther.OpCheckIndex; // End opcheck index
							return state.OpCheckIndex;
						}
					}
					return OpCheckIndex;
				}
			}

			#endregion
		}
		/// <summary>
		/// Data used in <see cref="MatchState"/> for a single quantifier match.
		/// </summary>
		private class QuantifierMatch {
			#region Fields

			/// <summary>
			/// The captured IL groups.
			/// </summary>
			public ILGroup[] Groups { get; set; }
			/// <summary>
			/// The captured IL operands.
			/// </summary>
			public ILOperand[] Operands { get; set; }
			/// <summary>
			/// The ending instruction position of this match.
			/// </summary>
			public int End { get; set; }

			// Group:
			/// <summary>
			/// The stack used to keep track of the group's state.
			/// </summary>
			public Stack<MatchState> Stack { get; set; }
			/// <summary>
			/// The queue used to keep track of the group's next alternatives.
			/// </summary>
			public Queue<ILCheck> Alternatives { get; set; }

			#endregion

			#region Properties

			/// <summary>
			/// Gets if this group match can backtrack to retry for another match.
			/// </summary>
			public bool CanBacktrack => Stack.Count > 0 || Alternatives.Count > 0;
			/// <summary>
			/// Gets the number of match states in this group match's stack.
			/// </summary>
			public int Count => Stack.Count;
			/// <summary>
			/// Gets if this group match has any alternatives.
			/// </summary>
			public bool HasAlternatives => Alternatives.Count != 0;

			#endregion

			/*public QuantifierMatch CloneCaptures() {
				return Copy(new QuantifierMatch());
			}*/
			public QuantifierMatch CopyTo(QuantifierMatch match) {
				if (match.Groups   == null)
					match.Groups   = new ILGroup  [Groups.Length];
				if (match.Operands == null)
					match.Operands = new ILOperand[Operands.Length];
				Array.Copy(Groups,   match.Groups,   Groups.Length);
				Array.Copy(Operands, match.Operands, Operands.Length);
				return match;
			}
		}
	}
}
