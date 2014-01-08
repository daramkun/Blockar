using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Common
{
	internal class EncodingChecker
	{
		private static int CompareArray ( byte [] a, byte [] b )
		{
			int diff = 0;
			for ( int i = 0; i < a.Length; ++i )
				diff += ( a [ i ] != b [ i ] ) ? 1 : 0;
			return diff;
		}

		public static void Check ( Stream stream, out Encoding encoding, out int skipByte )
		{
			byte [] buffer;
			long currentPosition = stream.Position;

			skipByte = 4;
			buffer = new byte [ 4 ];
			stream.Read ( buffer, 0, 4 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0, 0, 0xFE, 0xFF } ) == 0 ) { encoding = Encoding.GetEncoding ( "utf-32" ); return; }
			if ( CompareArray ( buffer, new byte [] { 0xFF, 0xFE, 0, 0 } ) == 0 ) { encoding = Encoding.GetEncoding ( "utf-32" ); return; }

			skipByte = 2;
			buffer = new byte [ 2 ];
			stream.Read ( buffer, 0, 2 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0xFE, 0xFF } ) == 0 ) { encoding = Encoding.BigEndianUnicode; return; }
			if ( CompareArray ( buffer, new byte [] { 0xFF, 0xFE } ) == 0 ) { encoding = Encoding.Unicode; return; }

			skipByte = 3;
			buffer = new byte [ 3 ];
			stream.Read ( buffer, 0, 3 );
			stream.Position = currentPosition;

			if ( CompareArray ( buffer, new byte [] { 0xEF, 0xBB, 0xBF } ) == 0 ) { encoding = Encoding.UTF8; return; }

			skipByte = 0;
			encoding = Encoding.UTF8;
		}

		public static Encoding Check ( Stream stream, out int skipByte )
		{
			Encoding encoding;
			Check ( stream, out encoding, out skipByte );
			return encoding;
		}

		public static Encoding Check ( Stream stream )
		{
			Encoding encoding;
			int skipByte;
			Check ( stream, out encoding, out skipByte );
			return encoding;
		}
	}
}
