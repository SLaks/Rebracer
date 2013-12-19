using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
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

		const string SolutionItems = "Solution Items";
		public static Project GetSolutionItems(this Solution solution) {
			return solution.Projects
						   .OfType<Project>()
						   .FirstOrDefault(p => p.Name.Equals(SolutionItems, StringComparison.OrdinalIgnoreCase))
						?? ((Solution2)solution).AddSolutionFolder(SolutionItems);
		}

		///<summary>Finds the actual bounds of a Visual Studio window in logical pixels.</summary>
		/// <remarks>When a window is maximized, its position properties return non-maximized values.  This method works around that.</remarks>
		public static Rect ActualBounds(this EnvDTE.Window window) {
			if (window.WindowState != vsWindowState.vsWindowStateMaximize)
				return new Rect(window.Left, window.Top, window.Width, window.Height).DeviceToLogicalUnits();

			var screen = Screen.FromHandle(new IntPtr(window.HWnd));
			return new Rect(screen.WorkingArea.Left, screen.WorkingArea.Top,
							screen.WorkingArea.Width, screen.WorkingArea.Height).DeviceToLogicalUnits();
		}
	}
}
