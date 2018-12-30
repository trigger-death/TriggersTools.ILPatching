using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TriggersTools.ILPatching.RegularExpressions {
	/// <summary>
	/// Used to compile and flatten <see cref="ILCheck"/>s into an array of <see cref="ILCheck"/>s.
	/// </summary>
	internal class ILRegexCompiler {

		/// <summary>
		/// Compiles and flattens the array of <see cref="ILCheck"/>s into an array of
		/// <see cref="ILCheck"/>s.
		/// </summary>
		/// <param name="checks">The checks to compile.</param>
		/// <param name="groupCount">The output number of group captures that were found.</param>
		/// <param name="operandCount">The output number of operand captures that were found.</param>
		/// 
		/// <exception cref="ILRegexException">
		/// A quantifier is improperly placed. Or a group's start or end are mismatched.
		/// </exception>
		public static ILCheck[] Compile(IEnumerable<ILCheck> checks, out int groupCount,
			out int operandCount)
		{
			groupCount = 0;
			operandCount = 0;
			List<ILCheck> opChecks = new List<ILCheck>(); // + RegexStart

			// Used to store the initial state without having to
			// add special behavior when you're the first match state.
			opChecks.Add(new ILCheck(OpChecks.Nop) { OpCheckIndex = opChecks.Count });

			ILCheck matchStart = new ILCheck(OpChecks.GroupStart) {
				CaptureIndex = groupCount++,
			};
			ILCheck matchEnd = new ILCheck(OpChecks.GroupEnd);

			// Entire regular expression is also treated as its own group.
			CompilePattern(opChecks, checks, matchStart, matchEnd, ref groupCount, ref operandCount);

			return opChecks.ToArray();
		}

		private static void CompilePattern(List<ILCheck> opChecks, IEnumerable<ILCheck> checks,
			ILCheck matchStart, ILCheck matchEnd, ref int groupCount, ref int operandCount)
		{
			matchEnd.CaptureIndex = matchStart.CaptureIndex;
			matchEnd.CaptureName = matchStart.CaptureName;
			matchStart.OpCheckIndex = opChecks.Count;
			matchStart.GroupOther = matchEnd;
			matchEnd.GroupOther = matchStart;

			opChecks.Add(matchStart);

			Stack<ILCheck> groupStack = new Stack<ILCheck>();
			Stack<List<ILCheck>> alternativesStack = new Stack<List<ILCheck>>();
			alternativesStack.Push(new List<ILCheck>());

			ILCheck lastCheck = matchStart;
			foreach (ILCheck current in checks) {
				ILCheck check = current;
				// No need to clone quantifiers
				if (check.Code == OpChecks.Quantifier) {
					if (lastCheck == null)
						throw new ILRegexException($"Unexpected quantifier check {check.Quantifier} at beginning of pattern!");
					else if (lastCheck.Code == OpChecks.GroupStart)
						throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to group start {lastCheck}!");
					else if (lastCheck.Code == OpChecks.Alternative)
						throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to altervative {lastCheck}!");
					else if (!lastCheck.Quantifier.IsOne)
						throw new ILRegexException($"Cannot attach quantifier {check.Quantifier} to an already quantified check {lastCheck}!");

					lastCheck.Quantifier = check.Quantifier;
					if (lastCheck.GroupOther != null)
						lastCheck.GroupOther.Quantifier = check.Quantifier;
					continue;
				}

				// Clone the opcheck so changes can be made to it.
				check = check.Clone();
				if (check.Code == OpChecks.GroupStart) {
					if (check.IsCapture)
						check.CaptureIndex = groupCount++;

					check.OpCheckIndex = opChecks.Count;
					opChecks.Add(check);
					groupStack.Push(check);
					alternativesStack.Push(new List<ILCheck>());
				}
				else if (check.Code == OpChecks.GroupEnd) {
					if (groupStack.Count == 0)
						throw new ILRegexException("Cannot end group without a group start!");

					FillEmptyGroup(opChecks, lastCheck, check);
					ILCheck flatGroupStart = groupStack.Pop();
					ILCheck flatGroupEnd = check;
					ILCheck[] flatAlts = alternativesStack.Pop().ToArray();

					flatGroupEnd.OpCheckIndex = opChecks.Count;
					flatGroupEnd.CaptureIndex = flatGroupStart.CaptureIndex;
					flatGroupEnd.CaptureName = flatGroupStart.CaptureName;
					flatGroupStart.GroupOther = flatGroupEnd;
					flatGroupEnd.GroupOther = flatGroupStart;
					flatGroupStart.Alternatives = flatAlts;
					flatGroupEnd.Alternatives = flatAlts;
					flatGroupStart.Quantifier = flatGroupEnd.Quantifier;

					opChecks.Add(flatGroupEnd);
				}
				else {
					// Incase of (| or ||
					if (check.Code == OpChecks.Alternative) {
						FillEmptyGroup(opChecks, lastCheck, check);
						alternativesStack.Peek().Add(check);
					}

					check.OpCheckIndex = opChecks.Count;
					opChecks.Add(check);
					if (check.Code == OpChecks.Operand && check.IsCapture)
						check.CaptureIndex = operandCount++;
				}
				lastCheck = check;
			}
			if (groupStack.Count != 0)
				throw new ILRegexException($"Missing {groupStack.Count} group ends!");

			// Incase of () or |)
			FillEmptyGroup(opChecks, lastCheck, matchEnd);
			
			matchEnd.OpCheckIndex = opChecks.Count;
			ILCheck[] alts = alternativesStack.Pop().ToArray();
			matchStart.Alternatives = alts;
			matchEnd.Alternatives = alts;

			opChecks.Add(matchEnd);
		}

		private static void FillEmptyGroup(List<ILCheck> opChecks, ILCheck lastOpCheck, ILCheck opCheck) {
			// TODO: This may be needed, let's find out
			if (lastOpCheck.Code == OpChecks.GroupStart || lastOpCheck.Code == OpChecks.Alternative) {
				opChecks.Add(new ILCheck(OpChecks.Nop) { OpCheckIndex = opChecks.Count });
			}
		}
	}
}
