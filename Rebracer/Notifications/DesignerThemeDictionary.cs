using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Rebracer.Notifications {
	public class DesignerThemeDictionary : DeferredResourceDictionaryBase {

		// We must access everything from these classes using dynamic due to NoPIA conflicts.
		// The compiler gives some errors since we do not have the right PIA, and the runtime
		// gives more errors because NoPIA doesn't unify for managed implementations.
		dynamic currentTheme;
		readonly dynamic service;
		public DesignerThemeDictionary() {
			if (ServiceProvider.GlobalProvider.GetService(new Guid("FD57C398-FDE3-42c2-A358-660F269CBE43")) != null)
				return; // Do nothing when hosted in VS
			ServiceProviderMock.Initialize();
			service = Activator.CreateInstance(Type.GetType("Microsoft.VisualStudio.Platform.WindowManagement.ColorThemeService, Microsoft.VisualStudio.Platform.WindowManagement"));
			ThemeIndex = 0;
		}
		int themeIndex;
		public int ThemeIndex {
			get { return themeIndex; }
			set { themeIndex = value; LoadTheme(value); }
		}
		public void LoadTheme(int index) {
			if (service == null)
				return;
			Clear();

			currentTheme = service.Themes[index % service.Themes.Count];
			foreach (ColorName colorName in service.ColorNames) {
				IVsColorEntry vsColorEntry = currentTheme[colorName];
				if (vsColorEntry != null) {
					if (vsColorEntry.BackgroundType != 0) {
						ThemeResourceKey brushKey = new ThemeResourceKey(vsColorEntry.ColorName.Category, vsColorEntry.ColorName.Name, ThemeResourceKeyType.BackgroundBrush);
						ThemeResourceKey colorKey = new ThemeResourceKey(vsColorEntry.ColorName.Category, vsColorEntry.ColorName.Name, ThemeResourceKeyType.BackgroundColor);
						Add(brushKey, brushKey);
						Add(colorKey, colorKey);
						int num = VsColorFromName(colorName);
						if (num != 0) {
							Add(VsColors.GetColorKey(num), colorKey);
							Add(VsBrushes.GetBrushKey(num), brushKey);
						}
					}
					if (vsColorEntry.ForegroundType != 0) {
						ThemeResourceKey brushKey = new ThemeResourceKey(vsColorEntry.ColorName.Category, vsColorEntry.ColorName.Name, ThemeResourceKeyType.ForegroundBrush);
						ThemeResourceKey colorKey = new ThemeResourceKey(vsColorEntry.ColorName.Category, vsColorEntry.ColorName.Name, ThemeResourceKeyType.ForegroundColor);
						Add(brushKey, brushKey);
						Add(colorKey, colorKey);
					}
				}
			}
		}

		// Microsoft.VisualStudio.Platform.WindowManagement.ColorNameTranslator
		static int VsColorFromName(ColorName colorName) {
			int result;
			if (colorName.Category == EnvironmentColors.Category && VsColors.TryGetColorIDFromBaseKey(colorName.Name, out result)) {
				return result;
			}
			return 0;
		}

		protected override uint GetRgbaColorValue(ThemeResourceKey key) {
			var entry = currentTheme[new ColorName { Category = key.Category, Name = key.Name }];
			switch (key.KeyType) {
				case ThemeResourceKeyType.ForegroundColor:
				case ThemeResourceKeyType.ForegroundBrush:
					return entry.Foreground;
				case ThemeResourceKeyType.BackgroundColor:
				case ThemeResourceKeyType.BackgroundBrush:
					return entry.Background;
				default:
					throw new InvalidEnumArgumentException("key", (int)key.KeyType, typeof(ThemeResourceKeyType));
			}
		}
	}
}
namespace Microsoft.Internal.VisualStudio.Shell.Interop {

	[CompilerGenerated, Guid("0D915B59-2ED7-472A-9DE8-9161737EA1C5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface SVsColorThemeService {
	}
	[CompilerGenerated, Guid("EAB552CF-7858-4F05-8435-62DB6DF60684"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface IVsColorThemeService {
		IVsColorThemes Themes {
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
		IVsColorNames ColorNames {
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
		IVsColorTheme CurrentTheme {
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
		void NotifyExternalThemeChanged();
		uint GetCurrentVsColorValue([In] int vsSysColor);
		uint GetCurrentColorValue([In] ref Guid rguidColorCategory, [MarshalAs(UnmanagedType.LPWStr)] [In] string pszColorName, [In] uint dwColorType);
		uint GetCurrentEncodedColor([In] ref Guid rguidColorCategory, [MarshalAs(UnmanagedType.LPWStr)] [In] string pszColorName, [In] uint dwColorType);
	}
	[CompilerGenerated, Guid("98192AFE-75B9-4347-82EC-FF312C1995D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface IVsColorThemes {
		IVsColorTheme this[[In] int index] {
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
		int Count { get; }
		[return: MarshalAs(UnmanagedType.Interface)]
		IVsColorTheme GetThemeFromId([In] Guid ThemeId);
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler")]
		IEnumerator GetEnumerator();
	}
	[CompilerGenerated, Guid("92144F7A-61DE-439B-AA66-13BE7CDEC857"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface IVsColorNames {
		ColorName this[[In] int index] { get; }
		int Count { get; }
		ColorName GetNameFromVsColor([In] int vsSysColor);
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.EnumeratorToEnumVariantMarshaler")]
		IEnumerator GetEnumerator();
	}
	[CompilerGenerated, Guid("413D8344-C0DB-4949-9DBC-69C12BADB6AC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface IVsColorTheme {
		IVsColorEntry this[[In] ColorName Name] {
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}
		Guid ThemeId { get; }
		string Name {
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}
		bool IsUserVisible { get; }
		void Apply();
	}
	[CompilerGenerated, Guid("BBE70639-7AD9-4365-AE36-9877AF2F973B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface IVsColorEntry {
		ColorName ColorName { get; }
		byte BackgroundType { get; }
		byte ForegroundType { get; }
		uint Background { get; }
		uint Foreground { get; }
		uint BackgroundSource { get; }
		uint ForegroundSource { get; }
	}

	[CompilerGenerated, TypeIdentifier("EF2A7BE1-84AF-4E47-A2CF-056DF55F3B7A", "Microsoft.Internal.VisualStudio.Shell.Interop.ColorName")]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct ColorName {
		public Guid Category;
		[MarshalAs(UnmanagedType.BStr)]
		public string Name;
	}
}