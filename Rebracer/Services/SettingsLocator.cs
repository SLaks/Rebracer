using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace SLaks.Rebracer.Services {

	///<summary>Finds the correct location for solution-specific and user-global Rebracer settings files.</summary>
	[Export]
	public class SettingsLocator {
		readonly string FileName = "Rebracer.xml";

		readonly string userFolder;

		[ImportingConstructor]
		public SettingsLocator(SVsServiceProvider sp) {
			userFolder = new ShellSettingsManager(sp).GetApplicationDataFolder(ApplicationDataFolder.RoamingSettings);
		}

		///<summary>Gets the path to the user global settings file, to be used in the absence of a solution settings file.</summary>
		public string UserSettingsFile { get { return Path.Combine(userFolder, FileName); } }

		///<summary>Gets the path to a solution-specific settings file.</summary>
		public string SolutionPath(Solution solution) {

			if (String.IsNullOrWhiteSpace(solution.FileName))
				return null;

			string root = Path.GetPathRoot(solution.FileName);
			string path = Path.GetDirectoryName(solution.FileName).Substring(root.Length);
			string file = Path.Combine(root, path, FileName);

			if (File.Exists(file))
				return file;

			int index = path.LastIndexOf(Path.DirectorySeparatorChar);

			while (index != -1) {

				path = path.Substring(0, index);
				file = Path.Combine(root, path, FileName);

				if (File.Exists(file))
					return file;

				index = path.LastIndexOf(Path.DirectorySeparatorChar);
			}

			if( index == -1 ) {
				file = Path.Combine(root, FileName);

				if (File.Exists(file))
					return file;
			}

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
