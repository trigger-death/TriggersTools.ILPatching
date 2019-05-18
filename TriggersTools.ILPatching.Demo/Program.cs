using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TriggersTools.ILPatching.RegularExpressions;

namespace TriggersTools.ILPatching.Demo {
	class Program {
		class NameTreeItem {
			public Dictionary<char, NameTreeItem> Items { get; } = new Dictionary<char, NameTreeItem>();

			public bool IsRoot => Letter == '\0';
			public char Letter { get; set; }
			public string Name { get; set; }
			public bool Exists { get; set; }

			public static NameTreeItem BuildTree(string[] names) {
				NameTreeItem root = new NameTreeItem(string.Empty, false);
				foreach (string name in names.OrderBy(n => n, StringComparer.InvariantCulture)) {
					if (name.Length == 0)
						root.Exists = true;
					else
						root.Traverse(name);
				}
				return root;
			}

			public static string BuildRegex(string[] names) {
				return BuildTree(names).BuildRegex();
			}
			public string BuildRegex() {
				StringBuilder str = new StringBuilder();
				BuildRegex(str, false);
				return str.ToString();
			}
			private void BuildRegex(StringBuilder str, bool includeLetter) {
				if (includeLetter)
					str.Append(Regex.Escape(Letter.ToString()));
				var first = Items.Values.FirstOrDefault();

				if (Items.Count == 0) {
					if (!Exists)
						throw new Exception("Unexpected!");
					return;
				}
				else if (Items.Count == 1) {
					if (first.Items.Count == 0 || !Exists) {
						first.BuildRegex(str, true);
					}
					else {
						str.Append("(?:");
						first.BuildRegex(str, true);
						str.Append(')');
					}
				}
				else {
					var groups = GroupSameItems();
					if (groups.Count == 1) {
						if (Exists && groups[0][0].Items.Count != 0)
							str.Append("(?:");
						BuildRange(str, groups[0]);
						if (Exists && groups[0][0].Items.Count != 0)
							str.Append(')');
					}
					else {
						str.Append("(?:");
						for (int i = 0; i < groups.Count; i++) {
							var group = groups[i];
							if (i != 0)
								str.Append('|');
							BuildRange(str, group);
						}
						str.Append(')');
					}
				}
				#region Old
				/*else {
					var singleItems = Items.Values.Where(i => i.Items.Count == 0);
					char startChar = '\0';
					char lastChar = '\0';
					int count = singleItems.Count();
					if (count > 1) {
						var notSingleItems = Items.Values.Where(i => i.Items.Count != 0);
						if (count != Items.Count)
							str.Append("(?:");
						str.Append('[');
						// Char range
						void FinishStart() {
							str.Append(Regex.Escape(startChar.ToString()));
							if (startChar != lastChar) {
								if (startChar + 1 != lastChar)
									str.Append('-');
								str.Append(Regex.Escape(lastChar.ToString()));
							}
						}
						foreach (var item in Items.Values) {
							if (item.Letter == lastChar + 1) {
								lastChar = item.Letter;
								continue;
							}
							else if (startChar != '\0') {
								FinishStart();
							}
							startChar = item.Letter;
							lastChar = startChar;
						}
						FinishStart();
						str.Append(']');
						if (count != Items.Count) {
							str.Append('|');
							str.Append(string.Join("|", notSingleItems.Select(i => i.BuildRegex())));
							str.Append(')');
						}
					}
					else {
						str.Append("(?:");
						str.Append(string.Join("|", Items.Values.Select(i => i.BuildRegex())));
						str.Append(')');
					}
				}*/
				#endregion
				if (Exists)
					str.Append('?');
			}
			private void BuildRange(StringBuilder str, List<NameTreeItem> group) {
				bool exists = group[0].Exists;
				if (group.Count != 1) {
					str.Append('[');
					char startChar = '\0';
					char lastChar = '\0';
					// Char range
					void FinishStart() {
						str.Append(Regex.Escape(startChar.ToString()));
						if (startChar != lastChar) {
							if (startChar + 1 != lastChar)
								str.Append('-');
							str.Append(Regex.Escape(lastChar.ToString()));
						}
					}
					foreach (var item in group) {
						if (item.Letter == lastChar + 1) {
							lastChar = item.Letter;
							continue;
						}
						else if (startChar != '\0') {
							FinishStart();
						}
						startChar = item.Letter;
						lastChar = startChar;
					}
					FinishStart();
					str.Append(']');
					if (group[0].Items.Count != 0)
						group[0].BuildRegex(str, false);
				}
				else {
					group[0].BuildRegex(str, true);
				}
				
			}
			private List<List<NameTreeItem>> GroupSameItems() {
				List<List<NameTreeItem>> groups = new List<List<NameTreeItem>>();
				foreach (var item in Items.Values) {
					bool grouped = false;
					foreach (var group in groups) {
						if (HasSameItems(item, group[0], true)) {
							group.Add(item);
							grouped = true;
							break;
						}
					}
					if (!grouped) {
						groups.Add(new List<NameTreeItem> { item });
					}
				}
				return groups;
			}
			private static bool HasSameItems(NameTreeItem a, NameTreeItem b, bool root) {
				if ((!root && a.Letter != b.Letter) || a.Exists != b.Exists || a.Items.Count != b.Items.Count)
					return false;
				using (var aEnumerator = a.Items.Values.GetEnumerator())
				using (var bEnumerator = b.Items.Values.GetEnumerator()) {
					while (aEnumerator.MoveNext() && bEnumerator.MoveNext()) {
						if (!HasSameItems(aEnumerator.Current, bEnumerator.Current, false))
							return false;
					}
				}
				return true;
			}

			private NameTreeItem(string name, bool exists) {
				Letter = (name.Length != 0 ? name[name.Length - 1] : '\0');
				Name = name;
				Exists = exists;
			}
			private void Traverse(string name) {
				char letter = name[Name.Length]; // name.Length > Name.Length
				bool isCurrent = (name.Length == Name.Length + 1);
				if (!Items.TryGetValue(letter, out NameTreeItem item)) {
					item = new NameTreeItem(name.Substring(0, Name.Length + 1), isCurrent);
					Items.Add(letter, item);
				}
				if (!isCurrent) {
					item.Traverse(name);
				}
			}
		}
		static void Main(string[] args) {
			string[] names = AnyOpCode.GetOpCodeNames();
			string regex = NameTreeItem.BuildRegex(names);
			//TextCopy.Clipboard.SetText(regex);
			TextCopy.Clipboard.SetText(regex.Replace(@"\", @"\\"));

			//TextCopy.Clipboard.SetText(string.Join(Environment.NewLine, ));
			var pattern = ILPattern.FromFile(@"C:\Users\Onii-chan\Source\C#\TriggersTools\TriggersTools.ILPatching\vscode-ilregex-language\ilregex.ilregex");
			pattern.Print();
			Console.WriteLine();
			Console.WriteLine("Hello World!");
			Console.Read();
		}
	}
}
