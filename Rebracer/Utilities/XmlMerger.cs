using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
					o.AddBeforeSelf(newNode);
					// Insert the requisite whitespace between the two nodes
					newNode.AddAfterSelf(newNode.GetPrecedingWhitespace());
					newIndex++;
				}

				// If this element has a replacement in the new set, use it.
				if (newIndex < newItems.Count && StringComparer.Ordinal.Compare(newItems[newIndex].Key, thisKey) == 0) {
					if (!changed && !XNode.DeepEquals(o, newItems[newIndex].Value))
						changed = true;
					o.ReplaceWith(newItems[newIndex].Value);
					newIndex++;
				}

				// If the container is not already sorted, sort it,
				// then try again.  Preserve any whitespace between
				// elements, as well as any trailing whitespace for
				// the parent's closing tag.
				if (StringComparer.Ordinal.Compare(thisKey, lastKey) < 0) {
					container.ReplaceNodes(
						oldItems.OrderBy(nameSelector)
								.SelectMany(elem => new XNode[] { elem.PreviousNode as XText, elem }),
						container.LastNode as XText
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
			// Add these nodes before any trailing whitespace
			var inserter = container.LastNode is XText
				? container.LastNode.AddBeforeSelf : new NodeInserter(container.Add);

			var separatingWhitespace = container.Elements().LastOrDefault().GetPrecedingWhitespace();
			for (; newIndex < newItems.Count; newIndex++)
				inserter(separatingWhitespace, newItems[newIndex].Value);
			return changed;
		}
		///<summary>A method to insert nodes into a LINQ to XML document.</summary>
		delegate void NodeInserter(params object[] content);
		private static XNode GetPrecedingWhitespace(this XElement element) {
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
	}
}
