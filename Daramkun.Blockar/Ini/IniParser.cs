using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Ini
{
	public static class IniParser
	{
		private enum ParseState
		{
			None,

			Section,

			Key,
			Value,
		}

		public static IEnumerable<IniSection> Parse ( string ini )
		{
			foreach ( IniSection s in Parse ( new MemoryStream ( Encoding.UTF8.GetBytes ( ini ) ) ) )
				yield return s;
		}

		public static IEnumerable<IniSection> Parse ( Stream stream )
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
					if ( section != null ) yield return section;
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

			yield return section;
		}

		private static string GetSectionTitle ( string line, int startIndex )
		{
			StringBuilder sb = new StringBuilder ();
			for ( ; startIndex < line.Length && line [ startIndex ] != ']'; ++startIndex )
				sb.Append ( line [ startIndex ] );
			return sb.ToString ();
		}

		private static string GetKey ( string line, ref int startIndex )
		{
			StringBuilder sb = new StringBuilder ();
				for ( ; startIndex < line.Length && line [ startIndex ] != '='; ++startIndex )
					sb.Append ( line [ startIndex ] );
			++startIndex;
			return sb.ToString ().Trim ();
		}

		private static string GetValue ( string line, int startIndex )
		{
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
