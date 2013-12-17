using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
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
			foreach (var containerElem in xml.Root.Elements("ToolsOptions").Elements("ToolsOptionsCategory").Elements("ToolsOptionsSubCategory")) {
				string category = containerElem.Parent.Attribute("name").Value;
				string subcategory = containerElem.Attribute("name").Value;
				Properties container;
				try {
					container = dte.Properties[category, subcategory];
				} catch (Exception ex) {
					logger.Log("Warning: Not saving unsupported category " + category + "/" + subcategory + " in existing settings file; you may be missing an extension.", ex);
					continue;
				}

				// Single ampersand to avoid short-circuiting
				changed = changed & XmlMerger.MergeElements(
					containerElem,
					container.Cast<Property>().Select(XmlValue),
					x => x.Attribute("name").Value
				);
			}
			return changed;
		}

		static XElement XmlValue(Property prop) {
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
		}

		///<summary>Reads settings from the XML file into Visual Studio's global settings.</summary>
		public void LoadSettings() {
			var xml = XDocument.Load(SettingsPath, LoadOptions.PreserveWhitespace);

			foreach (var subcategoryElem in xml.Root.Elements("ToolsOptions").Elements("ToolsOptionsCategory").Elements("ToolsOptionsSubCategory")) {
				string category = subcategoryElem.Parent.Attribute("name").Value;
				string subcategory = subcategoryElem.Attribute("name").Value;

				if (!KnownSettings.IsAllowed(category, subcategory)) {
					logger.Log("Warning: Not loading unsafe category " + category + "/" + subcategory + ".  You may have a malicious Rebracer.xml file.");
					continue;
				}

				Properties container;
				try {
					container = dte.Properties[category, subcategory];
				} catch (Exception ex) {
					logger.Log("Warning: Not loading unsupported category " + category + "/" + subcategory + " from settings file; you may be missing an extension.", ex);
					continue;
				}

				foreach (var property in subcategoryElem.Elements("PropertyValue")) {
					try {
						container.Item(property.Attribute("name").Value).Value = VsValue(property);
					} catch (Exception ex) {
						logger.Log("An error occurred while reading the setting " + category + "/" + subcategory + "#" + property.Attribute("name").Value + " from settings file.", ex);
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
				new XDeclaration("1.0", "utf8", "yes"),
				commentLines.Select(c => new XComment(c)),
				new XElement("UserSettings",
					new XElement("ToolsOptions",
			from t in KnownSettings.DefaultCategories
			group t by t.Item1 into cat
			select new XElement("ToolsOptionsCategory",
				new XAttribute("name", cat.Key),
				cat.Select(sc => new XElement("ToolsOptionsSubCategory", new XAttribute("name", sc.Item2)))
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