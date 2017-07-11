using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using SLaks.Rebracer.Utilities;

namespace SLaks.Rebracer.Services {
	///<summary>Loads and saves Visual Studio settings to an XML file.</summary>
	[Export]
	public class SettingsPersister {
		private readonly DTE dte;
		private readonly ILogger logger;

		[ImportingConstructor]
		public SettingsPersister(SVsServiceProvider sp, ILogger logger) {
			this.logger = logger;
			dte = (DTE)sp.GetService(typeof(DTE));
		}

		///<summary>Gets or sets the path to the XML file containing the persisted settings.</summary>
		public string SettingsPath { get; private set; }

		///<summary>Updates the XML file with the current Visual Studio settings.</summary>
		public bool SaveSettings() {
			using (var stream = File.Open(SettingsPath, FileMode.OpenOrCreate)) {
				var xml = XDocument.Load(stream, LoadOptions.PreserveWhitespace);

				if (!UpdateSettingsXml(xml))
					return false;

				dte.CheckOutFromSourceControl(SettingsPath);
				stream.SetLength(0);
				xml.Save(stream);
				logger.Log("Saved changed settings to " + SettingsPath);
				OnSettingsSaved();
				return true;
			}
		}

		private bool UpdateSettingsXml(XDocument xml) {
			bool changed = false;
			foreach (var section in SettingsSection.FromXmlSettingsFile(xml.Root)) {

				Properties container;
				try {
					container = dte.Properties(section.Item1);
				} catch (Exception ex) {
					logger.Log("Warning: Not saving unsupported category " + section.Item1 + " in existing settings file; you may be missing an extension.  Error: " + ex.Message);
					continue;
				}

				// Single (bitwise) or to avoid short-circuiting & always run merge
				changed = changed | XmlMerger.MergeElements(
					section.Item2,
					container.Cast<Property>().Select(p => XmlValue(section.Item1, p)).Where(x => x != null),
					x => x.Attribute("name").Value
				);
			}
			return changed;
		}

		XElement XmlValue(SettingsSection section, Property prop) {
			string name = prop.Name;
			if (KnownSettings.ShouldSkip(section, name))
				return null;

			object value;
			try {
				value = prop.Value;
			} catch (COMException ex) {
				logger.Log((string)("An error occurred while saving " + section + "#" + name + ": " + ex.Message));
				return null;
			} catch (InvalidOperationException) {
				// The InvalidOperationException is thrown when property is internal, read only or write only, so
				// property value cannot be set or get.
				return null;
			}
			var collection = value as ICollection;
			if (collection == null)
				return new XElement("PropertyValue", new XAttribute("name", name), value);

			return new XElement("PropertyValue",
				new XAttribute("name", name),
				new XAttribute("ArrayType", "VT_VARIANT"),
				new XAttribute("ArrayElementCount", collection.Count),
				((IEnumerable<object>)value)
					.Select((v, i) => new XElement("PropertyValue", new XAttribute("name", i), v))
			);
		}

		///<summary>Reads settings from the XML file into Visual Studio's global settings.</summary>
		private void LoadSettings() {
			var xml = XDocument.Load(SettingsPath, LoadOptions.PreserveWhitespace);
			bool modified = false;

			foreach (var section in SettingsSection.FromXmlSettingsFile(xml.Root)) {
				if (!KnownSettings.IsAllowed(section.Item1)) {
					logger.Log("Warning: Not loading unsafe category " + section.Item1 + ".  You may have a malicious Rebracer.xml file.");
					continue;
				}

				Properties container;
				try {
					container = dte.Properties(section.Item1);
				} catch (Exception ex) {
					logger.Log("Warning: Not loading unsupported category " + section.Item1 + " from settings file; you may be missing an extension.  Error: " + ex.Message);
					continue;
				}

				List<XElement> elements = section.Item2.Elements("PropertyValue").ToList();

				foreach (var property in elements) {
					string name = property.Attribute("name").Value;
					if (KnownSettings.ShouldSkip(section.Item1, name))
						continue;
					try {
						Property p;

						try {
							p = container.Item(name);
						} catch (ArgumentException ex) {
							if ((uint)ex.HResult == 0x80070057) // E_INVALIDARG, Property does not exists.
							{
								// This error occurs when the IDE property does not exist at this time. Non-existent properties 
								// are removed from the settings file.

								property.Remove();
								modified = true;
								continue;
							}
							logger.Log("An error occurred while reading the setting " + section.Item1 + "#" + name + " from settings file.  Error: " + ex.Message);
							continue;
						}

						p.Value = VsValue(property);

					} catch (COMException ex) {

						if ((uint)ex.HResult == 0x80020003) // DISP_E_MEMBERNOTFOUND
						{
							// MSDN: A return value indicating that the requested member does not exist, or the call to Invoke 
							// tried to set the value of a read-only property. So this is not error.

							continue;
						}

						logger.Log("An error occurred while reading the setting " + section.Item1 + "#" + name + " from settings file.  Error: " + ex.Message);
					} catch (Exception ex) {
						logger.Log("An error occurred while reading the setting " + section.Item1 + "#" + name + " from settings file.  Error: " + ex.Message);
					}
				}
			}

			if (modified) {
				xml.Save(SettingsPath, SaveOptions.DisableFormatting);
			}
		}
		static object VsValue(XElement elem) {
			if (elem.Elements().Any())
				return elem.Elements().Select(x => x.Value).ToArray();
			else
				return elem.Value;
		}

		///<summary>Creates and activates a new settings file at the specified location, seeding it with the current Visual Studio settings.</summary>
		/// <param name="path">The path to the file to create.</param>
		/// <param name="commentLines">Lines of description to insert in a comment on top of the file.</param>
		public void CreateSettingsFile(string path, params string[] commentLines) {
			var xml = new XDocument(
				new XDeclaration("1.0", "utf-8", "yes"),
				commentLines.Select(c => new XComment(c)),
				new XElement("UserSettings",
					new XElement("ToolsOptions",
			from section in KnownSettings.DefaultCategories
			where IsPresent(section)
			group section by section.Category into cat
			select new XElement("ToolsOptionsCategory",
				new XAttribute("name", cat.Key),
				cat.Select(sc => new XElement("ToolsOptionsSubCategory", new XAttribute("name", sc.Subcategory)))
			)
					)
				)
			);
			UpdateSettingsXml(xml);
			xml.Save(path);
			SettingsPath = path;
			OnSettingsFileCreated();
		}
		bool IsPresent(SettingsSection section) {
			try {
				dte.Properties(section);
				return true;
			} catch (COMException) { return false; } catch (NotImplementedException) { return false; }
		}

		///<summary>Loads an existing settings file.</summary>
		public void ActivateSettingsFile(string path) {
			if (!File.Exists(path))
				throw new FileNotFoundException("SettingsPersister.SettingsPath doesn't exist.", path);

			if (SettingsPath == path)
				return;

			var oldPath = SettingsPath;
			SettingsPath = path;
			logger.Log("Loading settings from " + path);
			LoadSettings();
			OnSettingsLoaded(new SettingsFileLoadedEventArgs(oldPath, path));

		}

		///<summary>Occurs when new settings are applied from a settings file.</summary>
		public event EventHandler<SettingsFileLoadedEventArgs> SettingsLoaded;
		///<summary>Raises the SettingsLoaded event.</summary>
		///<param name="e">An SettingsFileLoadedEventArgs object that provides the event data.</param>
		void OnSettingsLoaded(SettingsFileLoadedEventArgs e) {
			if (SettingsLoaded != null)
				SettingsLoaded(this, e);
		}
		///<summary>Occurs when settings are saved to a settings file.  This event is only raised if the file is actually modified.</summary>
		public event EventHandler SettingsSaved;
		///<summary>Raises the SettingsSaved event.</summary>
		void OnSettingsSaved() { OnSettingsSaved(EventArgs.Empty); }
		///<summary>Raises the SettingsSaved event.</summary>
		///<param name="e">An EventArgs object that provides the event data.</param>
		void OnSettingsSaved(EventArgs e) {
			if (SettingsSaved != null)
				SettingsSaved(this, e);
		}
		///<summary>Occurs when a new settings file is created.</summary>
		public event EventHandler SettingsFileCreated;
		///<summary>Raises the SettingsFileCreated event.</summary>
		void OnSettingsFileCreated() { OnSettingsFileCreated(EventArgs.Empty); }
		///<summary>Raises the SettingsFileCreated event.</summary>
		///<param name="e">An EventArgs object that provides the event data.</param>
		void OnSettingsFileCreated(EventArgs e) {
			if (SettingsFileCreated != null)
				SettingsFileCreated(this, e);
		}
	}
	public class SettingsFileLoadedEventArgs : EventArgs {
		public SettingsFileLoadedEventArgs(string oldPath, string newPath) {
			OldPath = oldPath;
			NewPath = newPath;
		}

		public string OldPath { get; private set; }
		public string NewPath { get; private set; }
	}
}