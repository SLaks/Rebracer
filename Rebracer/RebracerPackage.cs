using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SLaks.Rebracer {
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	///
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the 
	/// IVsPackage interface and uses the registration attributes defined in the framework to 
	/// register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]

	[ProvideAutoLoad(UIContextGuids80.NoSolution)]      // Load the default settings file if VS was closed with a solution open
	[ProvideAutoLoad(UIContextGuids80.SolutionExists)]

	// This attribute is used to register the information needed to show this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidRebracerPkgString)]
	public sealed class RebracerPackage : Package {
		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require 
		/// any Visual Studio service because at this point the package object is created but 
		/// not sited yet inside Visual Studio environment. The place to do all the other 
		/// initialization is the Initialize method.
		/// </summary>
		public RebracerPackage() {
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
		}


		/// <summary>
		/// Initializes the package.  This method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize() {
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			base.Initialize();

			var componentModel = (IComponentModel)GetService(typeof(SComponentModel));

			// This service registers event handlers in its ctor
			// and does not require further interaction.
			componentModel.GetService<Services.SolutionListener>();

			var logger = componentModel.GetService<Services.ILogger>();

			var locator = componentModel.GetService<Services.SettingsLocator>();
			// On first launch, populate the global settings file
			// before loading any solution settings.
			if (!File.Exists(locator.UserSettingsFile)) {
				logger.Log("Creating user settings file to store current global settings");
				componentModel.GetService<Services.SettingsPersister>().CreateSettingsFile(locator.UserSettingsFile,
					"Rebracer User Settings File",
					"This file contains your global Visual Studio settings.",
					"Rebracer uses this file to restore your settings after",
					"closing a solution that specifies its own settings.",
					"This file will be automatically updated by Rebracer as",
					"you change settings in Visual Studio"
				);
			}
			// If the global file already exists, wait for SolutionListener
			// to restore it after Visual Studio launches, for users 

			var mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
			foreach (var command in componentModel.GetExtensions<Services.CommandBase>()) {
				mcs.AddCommand(command.Command);
			}
		}
	}
}
