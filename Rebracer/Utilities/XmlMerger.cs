using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SLaks.Rebracer.Utilities {
	public static class XmlMerger {
		///<summary>Merges a collection of new XML elements into an existing parent element, ensuring that the results are sorted alphabetically.</summary>
		///<param name="container">The parent element containing the original elements to merge into.  If the merge encounters unsorted elements, the entire container will be sorted.</param>
		///<param name="newElements">The elements to merge in.</param>
		///<param name="nameSelector">A delegate to extract the name from an element to compare against.  Use this to sort by element name or a name attribute.</param>
		///<remarks>
		/// Any elements in the original that are not in <paramref name="newElements"/> will remain as-is.
		/// Elements that exist in both will be replaced by the new element.
		/// If the original container or <paramref name="newElements"/> have duplicate names, the behavior is undefined.
		///</remarks>
		public static void MergeElements(this XElement container, IEnumerable<XElement> newElements, Func<XElement, string> nameSelector) {
			var newItems = newElements.Select(e => new KeyValuePair<string, XElement>(nameSelector(e), e)).OrderBy(t => t.Key).ToList();

			// May not be sorted
			var oldItems = container.Elements().ToList();

			int newIndex = 0;

			string lastKey = null;
			foreach (var o in oldItems) {
				var thisKey = nameSelector(o);

				// Insert any new items that should come before this element
				while (newIndex < newItems.Count && newItems[newIndex].Key.CompareTo(thisKey) < 0) {
					o.AddBeforeSelf(newItems[newIndex].Value);
					newIndex++;
				}

				// If this element has a replacement in the new set, use it.
				if(newIndex < newItems.Count && newItems[newIndex].Key.CompareTo(thisKey) == 0) {
					o.ReplaceWith(newItems[newIndex].Value);
					newIndex++;
				}

				// If we finished inserting all of the new elements, stop early.
				// This means that we won't check that the rest of the container
				// is sorted; that is fine.
				if (newIndex == newItems.Count)
					return;

				// If the container is not already sorted, sort it,
				// then try again.
				if (thisKey.CompareTo(lastKey) < 0) {
					container.ReplaceNodes(oldItems.OrderBy(nameSelector));
					MergeElements(container, newElements, nameSelector);
					return;
				}

				lastKey = thisKey;
			}
			// Add any new items that go after the last item.
			for (; newIndex < newItems.Count; newIndex++)
				container.Add(newItems[newIndex].Value);
		}
	}
}
