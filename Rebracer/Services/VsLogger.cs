using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SLaks.Rebracer.Services {
	[Export(typeof(ILogger))]
	public class VsLogger : ILogger {
		private readonly IVsOutputWindowPane pane;

		[ImportingConstructor]
		public VsLogger(SVsServiceProvider sp) {
			var window = (IVsOutputWindow)sp.GetService(typeof(SVsOutputWindow));

			var guid = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
			ErrorHandler.ThrowOnFailure(window.GetPane(ref guid, out pane));
		}

		public void Log(string message) {
			ErrorHandler.ThrowOnFailure(pane.OutputStringThreadSafe(DateTime.Now + ": Rebracer: " + message));
		}

		public void Log(string message, Exception ex) {
			Log(message + "\n" + ex);
		}
	}
}
