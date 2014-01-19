using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace SLaks.Rebracer.Utilities {
	public static class XmlMerger {
		///<summary>Merges a collection of new XML elements into an existing parent element, ensuring that the results are sorted alphabetically.</summary>
		///<param name="container">The parent element containing the original elements to merge into.  If it contains out-of-order elements, the entire container will be sorted.</param>
		///<param name="newElements">The elements to merge in.</param>
		///<param name="nameSelector">A delegate to extract the name from an element to compare against.  Use this to sort by element name or a name attribute.</param>
		///<returns>True if the container was changed; false if all of the new elements were already present with the same values.</returns>
		///<remarks>
		/// Any elements in the original that are not in <paramref name="newElements"/> will remain as-is.
		/// Elements that exist in both will be replaced by the new element.
		/// If the original container or <paramref name="newElements"/> have duplicate names, the behavior is undefined.
		///</remarks>
		public static bool MergeElements(this XElement container, IEnumerable<XElement> newElements, Func<XElement, string> nameSelector) {
			var newItems = newElements
				.Select(e => new KeyValuePair<string, XElement>(nameSelector(e), e))
				.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();

			// May not be sorted
			var oldItems = container.Elements().ToList();

			int newIndex = 0;
			bool changed = false;

			string lastKey = null;
			foreach (var o in oldItems) {
				var thisKey = nameSelector(o);

				// Insert any new items that should come before this element
				while (newIndex < newItems.Count && StringComparer.Ordinal.Compare(newItems[newIndex].Key, thisKey) < 0) {
					changed = true;
					XElement newNode = newItems[newIndex].Value;

					// Insert the new element before any comments or
					// whitespace that precede this element, and add
					// the requisite separator whitespace before it.
					var precedingTrivia = o.GetPrecedingTrivia().FirstOrDefault();

					(precedingTrivia ?? o).AddBeforeSelf(newNode);
					newNode.AddBeforeSelf(newNode.GetPrecedingWhitespace());
					newIndex++;
				}

				// If this element has a replacement in the new set, use it.
				if (newIndex < newItems.Count && StringComparer.Ordinal.Compare(newItems[newIndex].Key, thisKey) == 0) {
					if (!changed && !DeepContentEqual(o, newItems[newIndex].Value))
						changed = true;
					o.ReplaceWith(newItems[newIndex].Value);
					IndentChildren(newItems[newIndex].Value);
					newIndex++;
				}

				// If the container is not already sorted, sort it,
				// then try again. Preserve whitespace and comments
				// between existing elements, as well as before the
				// parent's closing tag.
				if (StringComparer.Ordinal.Compare(thisKey, lastKey) < 0) {
					container.ReplaceNodes(
						oldItems.OrderBy(nameSelector)
								.SelectMany(elem => new object[] { elem.GetPrecedingTrivia(), elem }),
						container.Elements().Last().NodesAfterSelf()
					);
					MergeElements(container, newElements, nameSelector);
					// Because we sorted the existing elements, we certainly changed something
					return true;
				}

				lastKey = thisKey;
			}
			if (newIndex < newItems.Count)
				changed = true;

			// Add any new items that go after the last item.
			// Add these nodes immediately following the last
			// element, before trailing whitespace & comments
			var lastElement = container.Elements().LastOrDefault();
			// Use AddBeforeSelf() to preserve ordering.
			var inserter = lastElement != null && lastElement.NextNode != null
				? lastElement.NextNode.AddBeforeSelf : new NodeInserter(container.Add);

			var separatingWhitespace = lastElement.GetPrecedingWhitespace();
			for (; newIndex < newItems.Count; newIndex++)
				inserter(separatingWhitespace, newItems[newIndex].Value);
			return changed;
		}

		///<summary>Checks whether two elements are deeply equal, ignoring pure whitespace and comments.</summary>
		/// <remarks>Does not ignore insignificant whitespace within a string.</remarks>
		static bool DeepContentEqual(XElement first, XElement second) {
			if (first == second) return true;
			if (first == null || second == null) return false;

			if (first.Name != second.Name) return false;
			if (!SequenceEqual(first.Attributes(), second.Attributes(), (x, y) => x.Name == y.Name && x.Value == y.Value))
				return false;

			return SequenceEqual(ContentNodes(first), ContentNodes(second), (x, y) => {
				if (x.NodeType != y.NodeType)
					return false;
				if (x.NodeType == XmlNodeType.Element)
					return DeepContentEqual((XElement)x, (XElement)y);
				return XNode.DeepEquals(x, y);
			});
		}
		static IEnumerable<XNode> ContentNodes(this XContainer container) {
			return container.Nodes().Where(n => n.NodeType != XmlNodeType.Comment 
											&& (n.NodeType != XmlNodeType.Text || !string.IsNullOrWhiteSpace(((XText)n).Value))
										  );
		}

		// Copied from LINQ source; modified to accept a delegate.
		static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TSource, bool> comparer) {
			if (first == null) throw new ArgumentNullException("first");
			if (second == null) throw new ArgumentNullException("second");
			using (IEnumerator<TSource> e1 = first.GetEnumerator())
			using (IEnumerator<TSource> e2 = second.GetEnumerator()) {
				while (e1.MoveNext()) {
					if (!(e2.MoveNext() && comparer(e1.Current, e2.Current))) return false;
				}
				if (e2.MoveNext()) return false;
			}
			return true;
		}

		///<summary>Gets all whitespace and comment nodes before the specified element, until its preceding element.</summary>
		static IEnumerable<XNode> GetPrecedingTrivia(this XElement element) {
			var lastElem = element.ElementsBeforeSelf().LastOrDefault();
			if (lastElem == null)   // If it's the first element, take all preceding nodes
				return element.NodesBeforeSelf();
			else                    // Otherwise, take all nodes after the prior element.
				return lastElem.NodesAfterSelf().TakeWhile(n => n != element);
		}

		///<summary>A method to insert nodes into a LINQ to XML document.</summary>
		delegate void NodeInserter(params object[] content);
		///<summary>Gets the XText node containing the whitespace used to indent this element.  If there is no preceding whitespace, the following whitespace will be returned, if any.</summary>
		private static XText GetPrecedingWhitespace(this XElement element) {
			// If we started from an empty parent, there is no known whitespace
			if (element == null)
				return null;
			var sample = element.PreviousNode as XText
					  ?? element.NextNode as XText;
			// If there is no whitespace before or after the node, give up.
			if (sample == null)
				return null;
			return new XText(sample);
		}

		///<summary>Indents the children of an inserted element, based on the indentation of the parent element.</summary>
		public static void IndentChildren(this XElement parent) {
			var parentPrefix = parent.GetPrecedingWhitespace();
			if (parentPrefix == null)   // No known indent to start from
				return;

			// Copy the child list before we start mutating.
			// Looking at XContainer source code, this isn't
			// strictly necessary.
			var childElements = parent.Elements().ToList();
			if (childElements.Count == 0)
				return;
			parent.Add(parentPrefix);   // Indent the closing tag.

			var childPrefix = GetChildPrefix(parent.Depth(), parentPrefix.Value);
			if (string.IsNullOrEmpty(childPrefix))
				return;

			foreach (var child in childElements) {
				child.AddBeforeSelf(childPrefix);
				IndentChildren(child);
			}
		}

		public static int Depth(this XElement element) {
			if (element.Parent == null)
				return 0;
			return element.Parent.Depth() + 1;
		}

		private static string GetChildPrefix(int parentDepth, string parentPrefix) {
			string newLine = parentPrefix.Substring(0, parentPrefix.TakeWhile(c => c == '\r' || c == '\n').Count());
			parentPrefix = parentPrefix.Substring(newLine.Length);

			string levelIndent;
			if (parentDepth == 0)
				return newLine; // Don't know how to indent
			else
				levelIndent = parentPrefix.Substring(0, parentPrefix.Length / parentDepth);

			return newLine + string.Concat(Enumerable.Repeat(levelIndent, parentDepth + 1));
		}
	}
}
