using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfGrowlNotification {
	public partial class GrowlNotifications {
		private const byte MAX_NOTIFICATIONS = 4;
		private readonly ObservableCollection<Notification> buffer = new ObservableCollection<Notification>();
		private int count;

		public ObservableCollection<Notification> Notifications { get; private set; }

		public GrowlNotifications() {
			InitializeComponent();
			Notifications = new ObservableCollection<Notification>();
			NotificationsControl.DataContext = Notifications;
		}

		public void AddNotification(Notification notification) {
			notification.Id = count++;
			if (Notifications.Count + 1 > MAX_NOTIFICATIONS)
				buffer.Add(notification);
			else
				Notifications.Add(notification);

			//Show window if there're notifications
			if (Notifications.Count > 0 && !IsActive)
				Show();
		}

		public void RemoveNotification(Notification notification) {
			if (Notifications.Contains(notification))
				Notifications.Remove(notification);

			if (buffer.Count > 0) {
				Notifications.Add(buffer[0]);
				buffer.RemoveAt(0);
			}

			//Close window if there's nothing to show
			if (Notifications.Count < 1)
				Hide();
		}

		private void NotificationWindowSizeChanged(object sender, SizeChangedEventArgs e) {
			if (e.NewSize.Height > 5)
				return;
			var element = sender as FrameworkElement;
			RemoveNotification((Notification)element.Tag);
		}
	}
}
