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
		public string SettingsPath { get; set; }

		public void SaveSettings() {
			using (var stream = File.Open(SettingsPath, FileMode.OpenOrCreate)) {
				var xml = XDocument.Load(stream, LoadOptions.PreserveWhitespace);

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

					XmlMerger.MergeElements(
						containerElem,
						container.Cast<Property>().Select(XmlValue),
						x => x.Attribute("name").Value
					);
				}
				stream.SetLength(0);
				xml.Save(stream);
			}
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
	}
}
