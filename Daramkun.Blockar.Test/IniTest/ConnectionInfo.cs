using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Common;
using Daramkun.Blockar.Ini;

namespace Daramkun.Blockar.Test.IniTest
{
	public class ConnectionInfo : IIniSection
	{
		public string Name { get; set; }

		[Record]
		public string IPAddress { get; set; }
		[Record]
		public int Port { get; set; }
		[Record]
		public bool IsAlive { get; set; }

		public IniSection ToIniSection () { return IniProvider.ToIniSection ( this ); }
		public IIniSection FromIniSection ( IniSection data ) { IniProvider.FromIniSection ( this, data ); return this; }

		public override string ToString ()
		{
			return ToIniSection ().ToString ();
		}
	}
}
