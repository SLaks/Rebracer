using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			var xml = XDocument.Load(SettingsPath);

			foreach (var containerElem in xml.Root.Elements("ToolsOptions").Elements("ToolsOptionsCategory").Elements("ToolsOptionsSubCategory")) {
				string category = containerElem.Parent.Attribute("name").Value;
				string subcategory = containerElem.Attribute("name").Value;
				var container = dte.Properties[category, subcategory];

				XmlMerger.MergeElements(
					containerElem,
					container.Cast<Property>().Select(XmlValue),
					x => x.Attribute("name").Value
				);
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
			var xml = XDocument.Load(SettingsPath);

			foreach (var subcategoryElem in xml.Root.Elements("ToolsOptions").Elements("ToolsOptionsCategory").Elements("ToolsOptionsSubCategory")) {
				string category = subcategoryElem.Parent.Attribute("name").Value;
				string subCategory = subcategoryElem.Attribute("name").Value;

				if (!KnownSettings.IsAllowed(category, subCategory)) {
					logger.Log("Warning: Not loading unsafe category " + category + "/" + subCategory + ".  You may have a malicious Rebracer.xml file");
					continue;
				}

				var container = dte.Properties[category, subCategory];

				foreach (var property in subcategoryElem.Elements("PropertyValue")) {
					container.Item(property.Attribute("Name").Value).Value = VsValue(property);
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
