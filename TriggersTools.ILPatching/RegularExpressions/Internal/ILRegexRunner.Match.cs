using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TriggersTools.ILPatching.RegularExpressions {
	partial class ILRegexRunner {
		/// <summary>
		/// Creates a match state with the current state as the previous state and the specified opcheck.
		/// </summary>
		/// <param name="currentState">The current state to use as the previous state.</param>
		/// <param name="opCheck">The opcheck of the state.</param>
		/// <returns>The created match state.</returns>
		private MatchState CreateState(MatchState currentState, ILCheck opCheck) {
			return new MatchState {
				PreviousState = currentState,
				OpCheck = opCheck,
			};
		}

		/// <summary>
		/// Matches a group quantifier match for the first time.
		/// </summary>
		/// <param name="group">The group state to start matching for.</param>
		/// <returns>True if the match was successful, otherwise false.</returns>
		private bool MatchGroup(MatchState group) {
			QuantifierMatch match = group.LastMatch.CopyTo(new QuantifierMatch {
				End = group.End,
				Stack = new Stack<MatchState>(),
				Alternatives = new Queue<ILCheck>(group.OpCheck.Alternatives),
			});
			group.Matches.Push(match);
			level++;
			bool success = HandleGroupLoop(group, match);
			if (!success)
				group.Matches.Pop();
			level--;
			return success;
		}
		/// <summary>
		/// Continues a group quantifier match for the second+ time.
		/// </summary>
		/// <param name="group">The group state to start matching for.</param>
		/// <returns>True if the match was successful, otherwise false.</returns>
		private bool ContinueGroup(MatchState group) {
			QuantifierMatch match = group.Matches.Peek();
			level++;
			bool success = (HandleGroupBacktrack(group, match) && HandleGroupLoop(group, match));
			level--;
			return success;
		}
		/// <summary>
		/// Runs the match loop for the group state.
		/// </summary>
		/// <param name="group">The group state containing the checks.</param>
		/// <param name="match">The current group quantifier match being worked on.</param>
		/// <returns>True if the match was successful, otherwise false.</returns>
		private bool HandleGroupLoop(MatchState group, QuantifierMatch match) {
			int i;
			// The current state always locates the next opcheck to persue
			// Technically this while loop *should* never return false, as we'll always hit GroupEnd first.
			while ((i = group.LastOpCheckIndex + 1) < opChecks.Length) {
				ILCheck opCheck = opChecks[i];

				bool? result = HandleGroupOpCheck(group, match, opCheck);
				if (result.HasValue) {
					if (!result.Value) {
						// Match failed, and no more room to backtrack
						if (!HandleGroupBacktrack(group, match))
							return false;
					}
				}
				else {
					// We've completed this group quantifier match
					break;
				}
			}

			// Finalize the group by setting the state end
			// point and copying the most up to date captures
			var lastStateMatch = group.LastState.LastMatch;
			lastStateMatch.CopyTo(match);
			match.End = lastStateMatch.End;
			if (group.OpCheck.IsCapture) {
				// Add this group to the captures if it is a capture
				int start = group.SecondLastMatch.End;
				int end = match.End;
				match.Groups[group.OpCheck.CaptureIndex] = new ILGroup(
					instructions, start, end, group.OpCheck.CaptureName);
			}
			return true;
		}
		
		/// <summary>
		/// Handle backtracking in a group the group's states until it finds another path to persue.
		/// </summary>
		/// <param name="group">The group state containing the checks.</param>
		/// <param name="match">The current group quantifier match being worked on.</param>
		/// <returns>True if the group found a place to continue, false if the group match failed.</returns>
		private bool HandleGroupBacktrack(MatchState group, QuantifierMatch match)
		{
			while (match.Stack.Count != 0) {
				var nextState = match.Stack.Peek();
				if (Match(nextState)) {
					return true;
				}
				// Quantifier match failed or cannot continue so
				// we have to abandon progress and backtrack.
				match.Stack.Pop();
			}

			// We've depleted this stack, that means we backtracked
			// all the way to the beginning of the group.
			if (match.Alternatives.Count != 0) {
				// We have alternative routes available, start the next in line
				// by pushing a state pointing to the next opcheck index.
				ILCheck opCheck = match.Alternatives.Dequeue();
				var altState = CreateState(group.LastState, opCheck);
				altState.Matches.Push(altState.LastMatch.CopyTo(new QuantifierMatch {
					End = group.SecondLastMatch.End, // We're abandoning this match, get the second last one
				}));
				match.Stack.Push(altState);
				if (CanOutputDebug)
					Debug.WriteLine($"{DebugSpacing}Alternative<{opCheck.OpCheckIndex.ToString().PadLeft(2)}>");
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Handles an matching of an opcheck within the group.
		/// </summary>
		/// <param name="group">The group state containing the checks.</param>
		/// <param name="match">The current group quantifier match being worked on.</param>
		/// <param name="opCheck">The opcheck to match.</param>
		/// <returns>
		/// True if the match was successful, false if the match failed, otherwise null if the group has ended.
		/// </returns>
		private bool? HandleGroupOpCheck(MatchState group, QuantifierMatch match, ILCheck opCheck) {
			switch (opCheck.Code) {
			case OpChecks.Alternative:
			case OpChecks.GroupEnd:
				// Group Match Success
				return null; // Null signifies Finished

			case OpChecks.GroupStart:
				// Recurse Groups
				var nextGroup = CreateState(group.LastState, opCheck);
				if (Match(nextGroup)) {
					match.Stack.Push(nextGroup);
					return true;
				}
				return false;

			case OpChecks.Nop:
				// Do nothing
				var nopState = CreateState(group.LastState, opCheck);
				nopState.Matches.Push(nopState.LastMatch.CopyTo(new QuantifierMatch {
					End = nopState.End,
				}));
				match.Stack.Push(nopState);
				return true;

			default:
				var nextState = CreateState(group.LastState, opCheck);
				if (Match(nextState)) {
					match.Stack.Push(nextState);
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Matches a normal opcheck.
		/// </summary>
		/// <param name="state">The opcheck match state to match with.</param>
		/// <returns>True if the match was successful, otherwise false.</returns>
		private bool MatchOpCheck(MatchState state) {
			int instructionIndex = state.End;
			ILCheck opCheck = state.OpCheck;
			object operand;
			bool success = false;
			bool consuming = opCheck.IsConsuming;
			if (consuming && instructionIndex >= end)
				return false;
			Instruction instruction = instructions[instructionIndex];

			QuantifierMatch match = state.LastMatch.CopyTo(new QuantifierMatch {
				End = instructionIndex + (consuming ? 1 : 0),
			});

			switch (opCheck.Code) {
			// Normal OpChecks
			case OpChecks.OpCode:
				success = opCheck.OpCode == instruction.OpCode; break;
			case OpChecks.OpCodeOperand:
				success = instruction.EqualsInstruction(method, opCheck.OpCode, opCheck.Operand); break;

			case OpChecks.FieldName:
				if (instruction.Operand is FieldReference fieldRef) {
					string pattern = BuildPattern(opCheck.MemberName, 'f');
					success = Regex.IsMatch(fieldRef.FullName, pattern);
				}
				break;
			case OpChecks.MethodName:
				if (instruction.Operand is MethodReference methodRef) {
					string pattern = BuildPattern(opCheck.MemberName, 'm');
					success = Regex.IsMatch(methodRef.FullName, pattern);
				}
				break;
			case OpChecks.TypeName:
				if (instruction.Operand is TypeReference typeRef) {
					string pattern = BuildPattern(opCheck.MemberName, 'T');
					success = Regex.IsMatch(typeRef.FullName, pattern);
				}
				break;
			case OpChecks.CallSiteName:
				if (instruction.Operand is CallSite callSite) {
					string pattern = BuildPattern(opCheck.MemberName, 'C');
					success = Regex.IsMatch(callSite.FullName, pattern);
				}
				break;

			case OpChecks.Skip:
				success = true; break;
			case OpChecks.Start:
				if (options.HasFlag(ILRegexOptions.SearchBoundaries))
					success = instructionIndex == start;
				else
					success = instructionIndex == 0;
				break;
			case OpChecks.End:
				if (options.HasFlag(ILRegexOptions.SearchBoundaries))
					success = instructionIndex == end;
				else
					success = instructionIndex == instructions.Length;
				break;
			//case OpChecks.Predicate:
			//	success = opCheck.Predicate(instruction, method); break;
			
			// Operand OpChecks
			case OpChecks.OperandEquals:
				// Find the operand that we have already matched
				// and attempt to compare it to the instruction.
				ILOperand ilOperand = ILOperand.EmptyOperand;
				if (opCheck.CaptureName != null)
					ilOperand = Array.Find(match.Operands, o => o?.Name == opCheck.CaptureName);
				else if (opCheck.CaptureIndex >= 0 && opCheck.CaptureIndex < match.Operands.Length)
					ilOperand = match.Operands[opCheck.CaptureIndex];


				// Have we captured the requested operand?
				if (ilOperand != null && ilOperand.Success) {
					success = instruction.EqualsInstruction(method, opCheck.OpCode, ilOperand.Operand);
					break;
				}

				// If we haven't captured an operand, check the operand dictionary. 
				if (opCheck.CaptureName != null && operandDictionary != null &&
					operandDictionary.TryGetValue(opCheck.CaptureName, out operand))
				{
					success = instruction.EqualsInstruction(method, opCheck.OpCode, operand);
				}
				break;

			case OpChecks.Operand:
				if (instruction.OpCode == opCheck.OpCode) {
					operand = instruction.Operand;
					if (opCheck.OpCode.IsMulti)
						operand = instruction.GetOperand(method); // method == null returns instruction.Operand
					match.Operands[opCheck.CaptureIndex] =
						new ILOperand(instructions, instructionIndex, opCheck.CaptureName, operand);
					success = true;
				}
				break;
			}
			if (success)
				state.Matches.Push(match);
			return success;
		}
		private static string BuildPattern(string name, char type) {
			if (name.StartsWith("?"))
				return name.Substring(1);
			const string startPattern = @"(?:^| |\.)";
			//string namePattern = @"((?:[A-Za-z_]\w\.)*";
			const string genericPattern = @"(?:<[A-Za-z_]\w>)?";
			const string methodPattern = @"\(.*\)";
			string pattern = startPattern + Regex.Escape(name);
			switch (type) {
			case 'F': break;
			case 'T': pattern += genericPattern; break;
			case 'M': pattern += genericPattern + methodPattern; break;
			}
			return pattern;
		}
	}
}
