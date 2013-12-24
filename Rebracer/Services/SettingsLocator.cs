extern alias settings;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Settings = global::Microsoft.VisualStudio.Settings;

namespace SLaks.Rebracer.Services {

	///<summary>Finds the correct location for solution-specific and user-global Rebracer settings files.</summary>
	[Export]
	public class SettingsLocator {
		readonly string FileName = "Rebracer.xml";

		readonly string userFolder;

		[ImportingConstructor]
		public SettingsLocator(SVsServiceProvider sp) {
			userFolder = new ShellSettingsManager(sp).GetApplicationDataFolder(Settings.ApplicationDataFolder.RoamingSettings);
		}

		///<summary>Gets the path to the user global settings file, to be used in the absence of a solution settings file.</summary>
		public string UserSettingsFile { get { return Path.Combine(userFolder, FileName); } }

		///<summary>Gets the path to a solution-specific settings file.</summary>
		public string SolutionPath(Solution solution) {
			return Path.Combine(Path.GetDirectoryName(solution.FileName), FileName);
		}

		///<summary>Gets the path to the settings file to use for a specific solution, if any.</summary>
		public string GetActiveFile(Solution solution) {
			if (!solution.IsOpen || String.IsNullOrEmpty(solution.FileName))
				return UserSettingsFile;
			return new[] { SolutionPath(solution), UserSettingsFile }.FirstOrDefault(File.Exists) ?? UserSettingsFile;
		}
	}
}
