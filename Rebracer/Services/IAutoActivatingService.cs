using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLaks.Rebracer.Services {
	///<summary>Marks services that are automatically activated when Visual Studio launches.</summary>
	public interface IAutoActivatingService {
		void Activate();
	}
}
