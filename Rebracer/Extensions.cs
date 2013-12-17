using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using SLaks.Rebracer.Utilities;

namespace SLaks.Rebracer {
	static class Extensions {
		public static void CheckOutFromSourceControl(this DTE dte, string fileName) {
			if (dte.SourceControl.IsItemUnderSCC(fileName) && !dte.SourceControl.IsItemCheckedOut(fileName))
				dte.SourceControl.CheckOutItem(fileName);
		}

		public static Properties Properties(this DTE dte, SettingsSection section) {
			return dte.Properties[section.Category, section.Subcategory];
		}
	}
}
