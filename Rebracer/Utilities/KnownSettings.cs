using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLaks.Rebracer.Utilities {
	public static class KnownSettings {
		static readonly HashSet<string> unsafeSubcategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			// AutoSaveFile allows attackers to make users load all VS settings from a hostile network share
			"Import and Export Settings",
			// ProjectTemplatesLocation allows attackers to host pre-infected project templates from a hostile network share
			"ProjectsAndSolution",
			// HomePage could allow advertising; ViewSourceExternalProgram allows RCE
			"WebBrowser",
			// StartPageRSSUrl could allow advertising; also, it makes no sense for this to be per-solution
			"Startup"
		};

		///<summary>Checks whether a settings category is safe to load from untrusted data.</summary>
		///<returns>False if the category contains properties that could allow an attacker to do harm.</returns>
		public static bool IsAllowed(string category, string subcategory) {
			return !unsafeSubcategories.Contains(subcategory);
		}

		///<summary>The options categories that should be included by default when creating a new settings file.</summary>
		///<remarks>Existing files will use whatever categories exist in their XML.</remarks>
		public static readonly IReadOnlyCollection<Tuple<string, string>> DefaultCategories = new ReadOnlyCollection<Tuple<string, string>>(new[] {
			Tuple.Create("Environment", "TaskList"),
			Tuple.Create("TextEditor", "CSharp-Specific"),
			Tuple.Create("TextEditor", "JavaScript Specific"),
			Tuple.Create("TextEditor", "C/C++ Specific"),
			Tuple.Create("TextEditor", "TypeScript Specific"),
			Tuple.Create("TextEditor", "XAML Specific"),

			//TODO: Which of these categories are used by Venus (<= Dev11) & Libra (>= Dev12)?
			Tuple.Create("TextEditor", "HTML Specific"),
			Tuple.Create("TextEditor", "HTMLX Specific"),
		});
	}
}
