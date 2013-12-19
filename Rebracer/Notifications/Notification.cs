using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfGrowlNotification {
	public class Notification : INotifyPropertyChanged {
		private string message;
		public string Message {
			get { return message; }
			set { SetProperty(ref message, value); }
		}

		private int id;
		public int Id {
			get { return id; }
			set { SetProperty(ref id, value); }
		}

		private Uri imageUrl;
		public Uri ImageUrl {
			get { return imageUrl; }
			set { SetProperty(ref imageUrl, value); }
		}

		private string title;
		public string Title {
			get { return title; }
			set { SetProperty(ref title, value); }
		}

		private void SetProperty<T>(ref T property, T value, [CallerMemberName] string name = null) {
			if (EqualityComparer<T>.Default.Equals(property, value))
				return;
			property = value;
			OnPropertyChanged(name);
		}

		protected virtual void OnPropertyChanged(string propertyName) {
			var handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
	// For design-time data context
	public class NotificationCollection : ObservableCollection<Notification> { }
}