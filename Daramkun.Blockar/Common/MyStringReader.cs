using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Common
{
	internal class MyStringReader
	{
		string str;
		int pos;

		public int Position { get { return pos; } set { pos = value; } }

		public MyStringReader ( Stream stream, Encoding encoding = null )
		{
			StreamReader reader = new StreamReader ( stream, encoding ?? Encoding.UTF8 );
			str = reader.ReadToEnd ();
			pos = 0;
		}

		public char ReadChar () { return str [ pos++ ]; }
		public char [] ReadChars ( int len )
		{
			List<char> c = new List<char> ();
			for ( int i = 0; i < len;++i )
				c.Add ( ReadChar () );
			return c.ToArray ();
		}

		public void AppendString ( string str ) { this.str += str; }
	}
}
