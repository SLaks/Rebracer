using System;
using System.Diagnostics.CodeAnalysis;

namespace SLaks.Rebracer {
	static class GuidList {
		public const string guidRebracerPkgString = "bfc869c4-ae0f-467a-86a4-5d9401303490";
		public const string guidRebracerCmdSetString = "f4eae6a4-8dde-4fce-971c-9f621b2fefb4";

		public static readonly Guid guidRebracerCmdSet = new Guid(guidRebracerCmdSetString);
	}

	[SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This enum is used solely as a container for constants")]

	///<summary>Contains command IDs for commands defined by this package.  These values are defined in the vsct file.</summary>
	public enum PackageCommand {
		CreateSolutionSettingsFile = 0x100
	}
}