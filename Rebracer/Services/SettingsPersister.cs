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

				// Single ampersand to avoid short-circuiting
				changed = changed & XmlMerger.MergeElements(
					section.Item2,
					container.Cast<Property>().Select(XmlValue).Where(x => x != null),
					x => x.Attribute("name").Value
				);
			}
			return changed;
		}

		XElement XmlValue(Property prop) {
			try {
				if (prop.Value is Array) {
					return new XElement("PropertyValue",
						new XAttribute("name", prop.Name),
						new XAttribute("ArrayType", "VT_VARIANT"),
						new XAttribute("ArrayElementCount", ((ICollection)prop.Value).Count),
						((IEnumerable<object>)prop.Value)
							.Select((v, i) => new XElement("PropertyValue", new XAttribute("name", i), v))
					);
				}
				else
					return new XElement("PropertyValue", new XAttribute("name", prop.Name), prop.Value);
			} catch (COMException ex) {
				logger.Log("An error occurred while saving " + prop.Name + ": " + ex.Message);
				return null;
			}
		}

		///<summary>Reads settings from the XML file into Visual Studio's global settings.</summary>
		public void LoadSettings() {
			var xml = XDocument.Load(SettingsPath, LoadOptions.PreserveWhitespace);

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

				foreach (var property in section.Item2.Elements("PropertyValue")) {
					try {
						container.Item(property.Attribute("name").Value).Value = VsValue(property);
					} catch (Exception ex) {
						logger.Log("An error occurred while reading the setting " + section.Item1 + "#" + property.Attribute("name").Value + " from settings file.  Error: " + ex.Message);
					}
				}
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
			from t in KnownSettings.DefaultCategories
			group t by t.Category into cat
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
		}

		///<summary>Loads an existing settings file.</summary>
		public void ActivateSettingsFile(string path) {
			if (!File.Exists(path))
				throw new FileNotFoundException("SettingsPersister.SettingsPath doesn't exist.", path);

			if (SettingsPath == path)
				return;

			SettingsPath = path;
			logger.Log("Loading settings from " + path);
			LoadSettings();
		}
	}
}