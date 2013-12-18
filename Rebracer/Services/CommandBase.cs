using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Rebracer.Services {
	///<summary>A base class for a single Visual Studio menu command.</summary>
	public abstract class CommandBase {
		public OleMenuCommand Command { get; private set; }

		protected CommandBase(PackageCommand id) {
			Command = new OleMenuCommand((s, e) => Execute(), new CommandID(GuidList.guidRebracerCmdSet, (int)id));
		}

		protected abstract void Execute();
	}
}
