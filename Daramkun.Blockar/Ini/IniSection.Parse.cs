using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Ini
{
	partial class IniSection
	{
		private enum ParseState
		{
			None,

			Section,

			Key,
			Value,
		}

		public IniSection Parse ( Stream stream )
		{
			int skipByte;
			Encoding encoding = EncodingChecker.Check ( stream, out skipByte );
			stream.Position += skipByte;
			StreamReader iniString = new StreamReader ( stream, encoding );
			IniSection section = null;

			while ( stream.CanRead && !iniString.EndOfStream )
			{
				int i = 0;
				string line = iniString.ReadLine ();
				if ( line.Length == 0 ) continue;
				for ( ; i < line.Length; ++i )
				{
					char ch = line [ i ];
					if ( ch != ' ' && ch != '	' && ch != '\a' && ch != '\r' )
						break;
				}
				if ( line [ i ] == ';' ) continue;
				else if ( line [ i ] == '[' )
				{
					if ( section != null ) return section;
					section = new IniSection ();
					section.Name = GetSectionTitle ( line, i + 1 );
				}
				else
				{
					string key = GetKey ( line, ref i );
					string value = GetValue ( line, i );
					section.Add ( key, value );
				}
			}

			return section;
		}

		private string GetSectionTitle ( string line, int startIndex )
		{
			StringBuilder sb = new StringBuilder ();
			for ( ; startIndex < line.Length && line [ startIndex ] != ']'; ++startIndex )
				sb.Append ( line [ startIndex ] );
			return sb.ToString ();
		}

		private string GetKey ( string line, ref int startIndex )
		{
			StringBuilder sb = new StringBuilder ();
			for ( ; startIndex < line.Length && line [ startIndex ] != '='; ++startIndex )
				sb.Append ( line [ startIndex ] );
			++startIndex;
			return sb.ToString ().Trim ();
		}

		private string GetValue ( string line, int startIndex )
		{
			if ( line.Length == startIndex ) return "";

			StringBuilder sb = new StringBuilder ();
			for ( ; startIndex < line.Length; ++startIndex )
			{
				char ch = line [ startIndex ];
				if ( ch != ' ' && ch != '	' && ch != '\a' && ch != '\r' )
					break;
			}
			if ( line [ startIndex ] == '"' )
			{
				++startIndex;
				for ( ; startIndex < line.Length && line [ startIndex ] != '"'; ++startIndex )
					sb.Append ( line [ startIndex ] );
			}
			else
			{
				for ( ; startIndex < line.Length && ( line [ startIndex ] != '\n' && line [ startIndex ] != ';' ); ++startIndex )
					sb.Append ( line [ startIndex ] );
			}
			return sb.ToString ().Trim ();
		}
	}
}
