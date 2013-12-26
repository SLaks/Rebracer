using System;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SLaks.Rebracer.Utilities;

namespace Rebracer.Tests.UtilitiesTets {
	[TestClass]
	public class XmlMergerTests {
		static bool MergeElements(XElement container, params XElement[] newItems) {
			return XmlMerger.MergeElements(container, newItems, x => x.Name.LocalName);
		}

		[TestMethod]
		public void PopulateEmptyElement() {
			var container = new XElement("C");

			MergeElements(container,
				new XElement("c"),
				new XElement("a"),
				new XElement("b")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c")
			));
		}
		[TestMethod]
		public void MergeIntoElement() {
			var container = new XElement("C",
				new XElement("b"),
				new XElement("d"),
				new XElement("f")
			);

			MergeElements(container,
				new XElement("g"),
				new XElement("e"),
				new XElement("b"),
				new XElement("c"),
				new XElement("a")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c"),
				new XElement("d"),
				new XElement("e"),
				new XElement("f"),
				new XElement("g")
			));
		}
		[TestMethod]
		public void ReorderExistingElements() {
			var container = new XElement("C",
				new XElement("c"),
				new XElement("d"),
				new XElement("b")
			);

			MergeElements(container,
				new XElement("a"),
				new XElement("e")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c"),
				new XElement("d"),
				new XElement("e")
			));
		}
		[TestMethod]
		public void ElementsAreCaseSensitive() {
			var container = new XElement("C",
				new XElement("A"),
				new XElement("b")
			);

			MergeElements(container,
				new XElement("a"),
				new XElement("B")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("A"),
				new XElement("B"),
				new XElement("a"),
				new XElement("b")
			));
		}
		[TestMethod]
		public void DontStopReorderingAfterLastInsertion() {
			var container = new XElement("C",
				new XElement("a"),
				new XElement("c"),
				new XElement("e"),
				new XElement("d")
			);

			MergeElements(container,
				new XElement("c", new XElement("child", "Hi there!")),
				new XElement("b")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c", new XElement("child", "Hi there!")),
				new XElement("d"),
				new XElement("e")
			), "the merger should not stop checking for ordering after it finishes merging all new elements");
		}

		[TestMethod]
		public void OverwriteExistingElements() {
			var container = new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c")
			);

			MergeElements(container,
				new XElement("a", 42),
				new XElement("c", new XAttribute("Value", 67))
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a", 42),
				new XElement("b"),
				new XElement("c", new XAttribute("Value", 67))
			));
		}
		[TestMethod]
		public void OverwriteExistingOutOfOrderElement() {
			var container = new XElement("C",
				new XElement("c"),
				new XElement("b"),
				new XElement("a")
			);

			MergeElements(container,
				new XElement("a", 42)
			);

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a", 42),
				new XElement("b"),
				new XElement("c")
			));
		}

		[TestMethod]
		public void ReorderCountsAsChange() {
			var container = new XElement("C",
				new XElement("c"),
				new XElement("d"),
				new XElement("b")
			);

			MergeElements(container,
				new XElement("b")
			).Should().BeTrue();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("b"),
				new XElement("c"),
				new XElement("d")
			));
		}
		[TestMethod]
		public void IdenticalReplacementIsNotChange() {
			var container = new XElement("C",
				new XElement("b", new XAttribute("SomeProp", DateTime.Today), "Hi there!", new XElement("Deep", "Content")),
				new XElement("c"),
				new XElement("d")
			);

			MergeElements(container,
				new XElement("b", new XAttribute("SomeProp", DateTime.Today), "Hi there!", new XElement("Deep", "Content"))
			).Should().BeFalse();

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("b", new XAttribute("SomeProp", DateTime.Today), "Hi there!", new XElement("Deep", "Content")),
				new XElement("c"),
				new XElement("d")
			));
		}

		[TestMethod]
		public void NewElementsGetNewLines() {
			// Note two tabs before each element
			var source = @"<C>
		<b />
		<d />
</C>";
			var container = XElement.Parse(source, LoadOptions.PreserveWhitespace);
			MergeElements(container,new XElement("c"));
			container.ToString().Should().Be(@"<C>
		<b />
		<c />
		<d />
</C>", "new element in middle should have correct whitespace");

			MergeElements(container, new XElement("a"));
			container.ToString().Should().Be(@"<C>
		<a />
		<b />
		<c />
		<d />
</C>", "new element at beginning should have correct whitespace");

			MergeElements(container, new XElement("e"), new XElement("f"));
			container.ToString().Should().Be(@"<C>
		<a />
		<b />
		<c />
		<d />
		<e />
		<f />
</C>", "new elements at end should have correct whitespace");
		}

		[TestMethod]
		public void ReorderingNewElementsGetNewLines() {
			// Note two tabs before each element
			var source = @"<C>
		<d />
		<b />
</C>";
			var container = XElement.Parse(source, LoadOptions.PreserveWhitespace);
			var newSource = MergeElements(container, new XElement("a"), new XElement("c"), new XElement("e"));
			container.ToString().Should().Be(@"<C>
		<a />
		<b />
		<c />
		<d />
		<e />
</C>");
		}
		[TestMethod]
		public void ReplacedElementsPreserveWhitespace() {
			// Note two tabs before each element
			var source = @"<C>
		<a />
		<c />
</C>";
			var container = XElement.Parse(source, LoadOptions.PreserveWhitespace);
			var newSource = MergeElements(container,
				new XElement("c", "Hi!")
			);
			container.ToString().Should().Be(@"<C>
		<a />
		<c>Hi!</c>
</C>");
		}
	}
	// Stolen from https://github.com/dennisdoomen/fluentassertions/pull/35
	// TODO: Delete these classes after that is released.
	static class XElemAssertionExtensions {

		/// <summary>
		/// Asserts that the current <see cref="XElement"/> is equivalent to the <paramref name="expected"/> element,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="expected">The expected element</param>
		public static AndConstraint<XElementAssertions> BeEquivalentTo(this XElementAssertions @this, XElement expected) {
			return @this.BeEquivalentTo(expected, string.Empty);
		}

		/// <summary>
		/// Asserts that the current <see cref="XElement"/> is equivalent to the <paramref name="expected"/> element,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="expected">The expected element</param>
		/// <param name="reason">
		/// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
		/// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
		/// </param>
		/// <param name="reasonArgs">
		/// Zero or more objects to format using the placeholders in <see cref="reason" />.
		/// </param>
		public static AndConstraint<XElementAssertions> BeEquivalentTo(this XElementAssertions @this, XElement expected, string reason, params object[] reasonArgs) {
			Execute.Assertion
				.ForCondition(XNode.DeepEquals(@this.Subject, expected))
				.BecauseOf(reason, reasonArgs)
				.FailWith("Expected XML element {0} to be equivalent to {1}{reason}.",
					@this.Subject, expected);

			return new AndConstraint<XElementAssertions>(@this);
		}

		/// <summary>
		/// Asserts that the current <see cref="XElement"/> is not equivalent to the <paramref name="unexpected"/> element,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="unexpected">The unexpected element</param>
		public static AndConstraint<XElementAssertions> NotBeEquivalentTo(this XElementAssertions @this, XElement unexpected) {
			return @this.NotBeEquivalentTo(unexpected, string.Empty);
		}

		/// <summary>
		/// Asserts that the current <see cref="XElement"/> is not equivalent to the <paramref name="unexpected"/> element,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="unexpected">The unexpected element</param>
		/// <param name="reason">
		/// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
		/// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
		/// </param>
		/// <param name="reasonArgs">
		/// Zero or more objects to format using the placeholders in <see cref="reason" />.
		/// </param>
		public static AndConstraint<XElementAssertions> NotBeEquivalentTo(this XElementAssertions @this, XElement unexpected, string reason, params object[] reasonArgs) {
			Execute.Assertion
				.ForCondition(!XNode.DeepEquals(@this.Subject, unexpected))
				.BecauseOf(reason, reasonArgs)
				.FailWith("Did not expect XML element {0} to be equivalent to {1}{reason}.",
					@this.Subject, unexpected);

			return new AndConstraint<XElementAssertions>(@this);
		}


	}
	static class XDocAssertionExtensions {
		/// <summary>
		/// Asserts that the current <see cref="XDocument"/> is equivalent to the <paramref name="expected"/> document,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="expected">The expected document</param>
		public static AndConstraint<XDocumentAssertions> BeEquivalentTo(this XDocumentAssertions @this, XDocument expected) {
			return @this.BeEquivalentTo(expected, string.Empty);
		}

		/// <summary>
		/// Asserts that the current <see cref="XDocument"/> is equivalent to the <paramref name="expected"/> document,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="expected">The expected document</param>
		/// <param name="reason">
		/// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
		/// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
		/// </param>
		/// <param name="reasonArgs">
		/// Zero or more objects to format using the placeholders in <see cref="reason" />.
		/// </param>
		public static AndConstraint<XDocumentAssertions> BeEquivalentTo(this XDocumentAssertions @this, XDocument expected, string reason, params object[] reasonArgs) {
			Execute.Assertion
				.ForCondition(XNode.DeepEquals(@this.Subject, expected))
				.BecauseOf(reason, reasonArgs)
				.FailWith("Expected XML document {0} to be equivalent to {1}{reason}.",
					@this.Subject, expected);

			return new AndConstraint<XDocumentAssertions>(@this);
		}

		/// <summary>
		/// Asserts that the current <see cref="XDocument"/> is not equivalent to the <paramref name="unexpected"/> document,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="unexpected">The unexpected document</param>
		public static AndConstraint<XDocumentAssertions> NotBeEquivalentTo(this XDocumentAssertions @this, XDocument unexpected) {
			return @this.NotBeEquivalentTo(unexpected, string.Empty);
		}

		/// <summary>
		/// Asserts that the current <see cref="XDocument"/> is not equivalent to the <paramref name="unexpected"/> document,
		/// using its <see cref="XNode.DeepEquals()" /> implementation.
		/// </summary>
		/// <param name="unexpected">The unexpected document</param>
		/// <param name="reason">
		/// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion 
		/// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
		/// </param>
		/// <param name="reasonArgs">
		/// Zero or more objects to format using the placeholders in <see cref="reason" />.
		/// </param>
		public static AndConstraint<XDocumentAssertions> NotBeEquivalentTo(this XDocumentAssertions @this, XDocument unexpected, string reason, params object[] reasonArgs) {
			Execute.Assertion
				.ForCondition(!XNode.DeepEquals(@this.Subject, unexpected))
				.BecauseOf(reason, reasonArgs)
				.FailWith("Did not expect XML document {0} to be equivalent to {1}{reason}.",
					@this.Subject, unexpected);

			return new AndConstraint<XDocumentAssertions>(@this);
		}
	}
}
