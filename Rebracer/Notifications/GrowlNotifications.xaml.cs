using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

		private void NotificationWindowSizeChanged(object sender, SizeChangedEventArgs e) {
			if (e.NewSize.Height > 5)
				return;
			var element = sender as FrameworkElement;
			RemoveNotification((Notification)element.DataContext);
		}
	}
}
