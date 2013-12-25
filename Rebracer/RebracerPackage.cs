using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

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
	public sealed class RebracerPackage : Package, IVsShellPropertyEvents {
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

		uint shellPropertyCookie;
		IVsShell shellService;

		/// <summary>
		/// Initializes the package.  This method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override async void Initialize() {
			base.Initialize();

			shellService = GetService(typeof(SVsShell)) as IVsShell;

			if (shellService != null) {
				ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out shellPropertyCookie));
			} else {
				await Task.Delay(TimeSpan.FromSeconds(5));
				FullInitialize();
			}
		}

		// Wait for VS to fully load
		// http://blogs.msdn.com/b/vsxteam/archive/2008/06/09/dr-ex-why-does-getservice-typeof-envdte-dte-return-null.aspx
		public int OnShellPropertyChange([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPROPID")]int propid, object var) {
			var property = (__VSSPROPID)propid;
			if (property != __VSSPROPID.VSSPROPID_Zombie)
				return 0;

			// If we're still zombied, wait for the next event.
			if ((bool)var)
				return 0;
			ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(shellPropertyCookie));
			FullInitialize();

			return 0;
		}

		private void FullInitialize() {
			var componentModel = (IComponentModel)GetService(typeof(SComponentModel));

			foreach (var service in componentModel.GetExtensions<Services.IAutoActivatingService>())
				service.Activate();

			var mcs = (IMenuCommandService)GetService(typeof(IMenuCommandService));
			foreach (var command in componentModel.GetExtensions<Services.CommandBase>()) {
				mcs.AddCommand(command.Command);
			}
		}
	}
}
