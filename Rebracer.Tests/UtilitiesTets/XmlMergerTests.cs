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
		static void MergeElements(XElement container, params XElement[] newItems) {

			XmlMerger.MergeElements(container, newItems, x => x.Name.LocalName);
		}

		[TestMethod]
		public void PopulateEmptyElement() {
			var container = new XElement("C");

			MergeElements(container,
				new XElement("c"),
				new XElement("a"),
				new XElement("b")
			);

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
			);

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
		public void ReorderExistingElement() {
			var container = new XElement("C",
				new XElement("c"),
				new XElement("d"),
				new XElement("b")
			);

			MergeElements(container,
				new XElement("a"),
				new XElement("e")
			);

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c"),
				new XElement("d"),
				new XElement("e")
			));
		}
		[TestMethod]
		public void StopReorderingAfterLastInsertion() {
			var container = new XElement("C",
				new XElement("a"),
				new XElement("c"),
				new XElement("e"),
				new XElement("d")
			);

			MergeElements(container,
				new XElement("c", new XElement("child", "Hi there!")),
				new XElement("b")
			);

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c", new XElement("child", "Hi there!")),
				new XElement("e"),
				new XElement("d")
			), "the merger should stop checking for ordering after it finishes merging all new elements");
		}

		[TestMethod]
		public void OverwriteExistingElement() {
			var container = new XElement("C",
				new XElement("a"),
				new XElement("b"),
				new XElement("c")
			);

			MergeElements(container,
				new XElement("a", 42),
				new XElement("c", new XAttribute("Value", 67))
			);

			container.Should().BeEquivalentTo(new XElement("C",
				new XElement("a", 42),
				new XElement("b"),
				new XElement("c", new XAttribute("Value", 67))
			));
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
