using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Daramee.Blockar
{
	partial class BlockarObject
	{
		public static Version LibraryVersion => Assembly.GetAssembly (typeof (BlockarObject)).GetName ().Version;
		public static string TargetFramework
		{
			get
			{
#if NET20
				return ".NET Framework 2.0";
#elif NET45
				return ".NET Framework 4.5";
#elif NET48
				return ".NET Framework 4.8";
#elif NETSTANDARD2_0
				return ".NETstandard 2.0";
#elif NET50
				return ".NET 5.0";
#else
				return "Unknown Framework";
#endif
			}
		}
	}
}
