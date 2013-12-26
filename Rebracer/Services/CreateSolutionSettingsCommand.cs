using System;
using System.ComponentModel.Composition;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Rebracer.Services {
	[Export(typeof(CommandBase))]
	class CreateSolutionSettingsCommand : CommandBase {
		private readonly DTE dte;
		private readonly SettingsLocator locator;
		private readonly ILogger logger;
		private readonly SettingsPersister persister;

		[ImportingConstructor]
		public CreateSolutionSettingsCommand(SVsServiceProvider sp, ILogger logger, SettingsPersister persister, SettingsLocator locator)
			: base(PackageCommand.CreateSolutionSettingsFile) {
			this.logger = logger;
			this.locator = locator;
			this.persister = persister;

			dte = (DTE)sp.GetService(typeof(DTE));

			Command.BeforeQueryStatus += Command_BeforeQueryStatus;

			return;
		}

		private void Command_BeforeQueryStatus(object sender, EventArgs e) {
			UpdateCommandStatus();
		}

		void UpdateCommandStatus() {
			Command.Visible = dte.Solution.IsOpen
						   && !String.IsNullOrEmpty(dte.Solution.FileName)
						   && !File.Exists(locator.SolutionPath(dte.Solution));
		}

		protected override void Execute() {
			logger.Log("Creating solution settings file, starting with current global settings");
			persister.CreateSettingsFile(locator.SolutionPath(dte.Solution),
				"Rebracer Solution Settings File",
				"This file contains Visual Studio settings for " + Path.GetFileName(dte.Solution.FileName) + ".",
				"Rebracer uses this file to apply settings for this solution",
				"when the solution is opened.",
				"Install Rebracer from http://visualstudiogallery.msdn.microsoft.com/410e9b9f-65f3-4495-b68e-15567e543c58 ",
				"See https://github.com/SLaks/Rebracer for more information"
			);
			dte.Solution.GetSolutionItems().ProjectItems.AddFromFile(persister.SettingsPath);
		}
	}
}
