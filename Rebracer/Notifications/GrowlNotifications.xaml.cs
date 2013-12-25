using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

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
	// Stolen from Microsoft.VisualStudio.Shell.ViewManager
	// This is a version-dependent assembly, so I can't use
	// it at all.
	//http://twitter.com/jaredpar/status/415661754979717120
	public class TitleBarButton : Button {
		// Removed unused IsChecked property.
		public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.Register("PressedBackground", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty PressedBorderBrushProperty = DependencyProperty.Register("PressedBorderBrush", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty PressedBorderThicknessProperty = DependencyProperty.Register("PressedBorderThickness", typeof(Thickness), typeof(TitleBarButton));
		public static readonly DependencyProperty HoverBackgroundProperty = DependencyProperty.Register("HoverBackground", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty HoverBorderBrushProperty = DependencyProperty.Register("HoverBorderBrush", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty HoverBorderThicknessProperty = DependencyProperty.Register("HoverBorderThickness", typeof(Thickness), typeof(TitleBarButton));
		public static readonly DependencyProperty GlyphForegroundProperty = DependencyProperty.Register("GlyphForeground", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty HoverForegroundProperty = DependencyProperty.Register("HoverForeground", typeof(Brush), typeof(TitleBarButton));
		public static readonly DependencyProperty PressedForegroundProperty = DependencyProperty.Register("PressedForeground", typeof(Brush), typeof(TitleBarButton));

		public Brush PressedBackground {
			get { return (Brush)GetValue(PressedBackgroundProperty); }
			set { SetValue(PressedBackgroundProperty, value); }
		}
		public Brush PressedBorderBrush {
			get { return (Brush)GetValue(PressedBorderBrushProperty); }
			set { SetValue(PressedBorderBrushProperty, value); }
		}
		public Thickness PressedBorderThickness {
			get { return (Thickness)GetValue(PressedBorderThicknessProperty); }
			set { SetValue(PressedBorderThicknessProperty, value); }
		}
		public Brush HoverBackground {
			get { return (Brush)GetValue(HoverBackgroundProperty); }
			set { SetValue(HoverBackgroundProperty, value); }
		}
		public Brush HoverBorderBrush {
			get { return (Brush)GetValue(HoverBorderBrushProperty); }
			set { SetValue(HoverBorderBrushProperty, value); }
		}
		public Thickness HoverBorderThickness {
			get { return (Thickness)GetValue(HoverBorderThicknessProperty); }
			set { SetValue(HoverBorderThicknessProperty, value); }
		}
		public Brush GlyphForeground {
			get { return (Brush)GetValue(GlyphForegroundProperty); }
			set { SetValue(GlyphForegroundProperty, value); }
		}
		public Brush HoverForeground {
			get { return (Brush)GetValue(HoverForegroundProperty); }
			set { SetValue(HoverForegroundProperty, value); }
		}
		public Brush PressedForeground {
			get { return (Brush)GetValue(PressedForegroundProperty); }
			set { SetValue(PressedForegroundProperty, value); }
		}

		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
		static TitleBarButton() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TitleBarButton), new FrameworkPropertyMetadata(typeof(TitleBarButton)));
		}
	}
}
