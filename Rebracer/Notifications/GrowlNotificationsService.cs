using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using SLaks.Rebracer.Services;
using WPFGrowlNotification;

namespace SLaks.Rebracer.Notifications {
	[Export(typeof(INotificationService))]
	class GrowlNotificationsService : GrowlNotifications, INotificationService {
		private readonly DTE dte;

		[ImportingConstructor]
		public GrowlNotificationsService(SVsServiceProvider sp) {
			dte = (DTE)sp.GetService(typeof(DTE));
			dte.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
		}

		private void DTEEvents_OnStartupComplete() {
			var wih = new WindowInteropHelper(this);
			wih.Owner = new IntPtr(dte.MainWindow.HWnd);
		}

		private void UpdateLocation() {
			var dteBounds = dte.MainWindow.ActualBounds();

			Left = dteBounds.Right - Width - 20;
			Top = dteBounds.Top + 20;
		}

		public void ShowNotification(string title, string text) {
			UpdateLocation();
			AddNotification(new Notification {
				Title = title,
				Message = text,
				ImageUrl = "pack://application:,,,/Rebracer;component/Resources/Rebracer-100.png"
			});
		}
	}
}
