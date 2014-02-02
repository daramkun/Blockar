using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Ini
{
	public interface IIniSection
	{
		string Name { get; set; }

		IniSection ToIniSection ();
		IIniSection FromIniSection ( IniSection data );
	}
}
