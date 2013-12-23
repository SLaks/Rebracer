using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfGrowlNotification {
	public partial class GrowlNotifications {
		private const byte MAX_NOTIFICATIONS = 4;
		private readonly ObservableCollection<Notification> buffer = new ObservableCollection<Notification>();

		private readonly ObservableCollection<Notification> currentNotifications = new ObservableCollection<Notification>();

		public GrowlNotifications() {
			InitializeComponent();
			NotificationsControl.DataContext = currentNotifications;
		}

		public void AddNotification(Notification notification) {
			if (currentNotifications.Count + 1 > MAX_NOTIFICATIONS)
				buffer.Add(notification);
			else
				currentNotifications.Add(notification);

			//Show window if there're notifications
			if (currentNotifications.Count > 0 && !IsActive)
				Show();
		}

		public void RemoveNotification(Notification notification) {
			if (currentNotifications.Contains(notification))
				currentNotifications.Remove(notification);

			if (buffer.Count > 0) {
				currentNotifications.Add(buffer[0]);
				buffer.RemoveAt(0);
			}

			//Close window if there's nothing to show
			if (currentNotifications.Count < 1)
				Hide();
		}
		private void NotificationWindow_Loaded(object sender, RoutedEventArgs e) {
			var element = (Border)sender;

			// When the hide animation starts, wait until
			// it finishes, then remove the notification.
			// I wish I could listen the Completed event,
			// but I can only get it from a resource, and
			// I can't figure out how to find the target.
			SizeChangedEventHandler sizeChangedHandler = null;
			sizeChangedHandler = async (s, se) => {
				if (se.PreviousSize.Height == 0)
					return;
				element.SizeChanged -= sizeChangedHandler;

				while (element.Height > 0)
					await Task.Delay(500);
				RemoveNotification((Notification)element.DataContext);
			};
			element.SizeChanged += sizeChangedHandler;
		}
	}
}
