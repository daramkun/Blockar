using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Test.IniTest;
using Daramkun.Blockar.Test.JsonTest;

namespace Daramkun.Blockar.Test
{
	class Program
	{
		static void Main ( string [] args )
		{
			IniTester.Run ();
			Console.WriteLine ();
			JsonTester.Run ();
		}
	}
}
