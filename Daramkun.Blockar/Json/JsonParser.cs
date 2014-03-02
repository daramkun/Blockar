using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Json
{
	public static class JsonParser
	{
		private enum ParseState
		{
			None,

			Object,
			Array,

			Key,
			Value,
		}

		private static JsonContainer ParseString ( BinaryReader jsonString )
		{
			ParseState parseMode = ParseState.None;
			ParseState parseState = ParseState.None;
			Queue<object> tokenStack = new Queue<object> ();

			while ( jsonString.BaseStream.CanRead )
			{
				char rc = jsonString.ReadChar ();
				if ( rc == '{' && parseState != ParseState.Value ) { parseMode = ParseState.Object; parseState = ParseState.Key; }
				else if ( rc == '[' && parseState != ParseState.Value ) { parseMode = ParseState.Array; parseState = ParseState.Value; }
				else if ( ( rc == '{' || rc == '[' ) && parseState == ParseState.Value )
				{
					jsonString.BaseStream.Position -= 1;
					tokenStack.Enqueue ( ParseString ( jsonString ) );
				}
				else if ( ( rc == '}' && parseMode == ParseState.Object ) ) break;
				else if ( ( rc == ']' && parseMode == ParseState.Array ) ) break;
				else if ( rc == ' ' || rc == '	' || rc == '　' || rc == '\n' || rc == '\r' || rc == '\a' ) continue;
				else if ( rc == ',' || rc == ':' )
				{
					if ( parseMode == ParseState.Object && parseState == ParseState.Value && rc == ',' ) parseState = ParseState.Key;
					else if ( parseMode == ParseState.Object && parseState == ParseState.Key && rc == ':' ) parseState = ParseState.Value;
					else if ( parseMode == ParseState.Array && parseState == ParseState.Value && rc == ',' ) parseState = ParseState.Value;
					else throw new Exception ( "Invalid JSON document." );
				}
				else if ( rc == '"' ) tokenStack.Enqueue ( GetStringFromString ( jsonString ) );
				else if ( rc == 't' || rc == 'f' ) tokenStack.Enqueue ( GetBooleanFromString ( jsonString, rc ) );
				else if ( rc == 'n' ) tokenStack.Enqueue ( GetNullFromString ( jsonString ) );
				else if ( ( rc >= '0' && rc <= '9' ) || rc == '-' || rc == '+' ) tokenStack.Enqueue ( GetNumberFromString ( jsonString, rc ) );

				else throw new ArgumentException ( "Invalid JSON document." );
			}

			return BuildAndReturn ( parseMode, tokenStack );
		}

		private static JsonContainer ParseBinary ( BinaryReader jsonBinary, ParseState parseMode = ParseState.Object )
		{
			Queue<object> tokenStack = new Queue<object> ();
			bool isParsing = true;
			int dataSize = jsonBinary.ReadInt32 ();
			while ( jsonBinary.BaseStream.CanRead && isParsing )
			{
				BSONType rb = ( BSONType ) jsonBinary.ReadByte ();
				if ( parseMode == ParseState.Object ) tokenStack.Enqueue ( GetKeyFromBinary ( jsonBinary ) );
				else jsonBinary.ReadByte ();

				switch ( rb )
				{
					case BSONType.EndDoc: isParsing = false; break;
					case BSONType.Double: tokenStack.Enqueue ( jsonBinary.ReadDouble () ); break;
					case BSONType.String: tokenStack.Enqueue ( GetStringFromBinary ( jsonBinary ) ); break;
					case BSONType.Document: tokenStack.Enqueue ( ParseBinary ( jsonBinary, ParseState.Object ) ); break;
					case BSONType.Array: tokenStack.Enqueue ( ParseBinary ( jsonBinary, ParseState.Array ) ); break;
					case BSONType.BinaryData: tokenStack.Enqueue ( GetBinaryFromBinary ( jsonBinary ) ); break;
					case BSONType.Boolean: tokenStack.Enqueue ( jsonBinary.ReadByte () == 0 ? false : true ); break;
					case BSONType.UTCTime: tokenStack.Enqueue ( DateTime.FromFileTimeUtc ( jsonBinary.ReadInt64 () ) ); break;
					case BSONType.Null: tokenStack.Enqueue ( null ); break;
					case BSONType.Regexp: tokenStack.Enqueue ( new Regex ( GetStringFromBinary ( jsonBinary ) ) ); break;
					case BSONType.JavascriptCode: tokenStack.Enqueue ( GetStringFromBinary ( jsonBinary ) ); break;
					case BSONType.Integer: tokenStack.Enqueue ( jsonBinary.ReadInt32 () ); break;
					case BSONType.Integer64: tokenStack.Enqueue ( jsonBinary.ReadInt64 () ); break;

					default: throw new Exception ( "There is unsupport Data type." );
				}
			}

			return BuildAndReturn ( parseMode, tokenStack );
		}

		public static JsonContainer Parse ( Stream stream )
		{
			byte [] temp = new byte [ 1 ];
			stream.Read ( temp, 0, 1 );
			stream.Position = 0;

			if ( temp [ 0 ] == ( byte ) '{' || temp [ 0 ] == ( byte ) '[' || temp [ 0 ] == ( byte ) ' ' || temp [ 0 ] == ( byte ) '	' ||
				temp [ 0 ] == ( byte ) '\r' || temp [ 0 ] == ( byte ) '\n' )
			{
				return ParseString ( new BinaryReader ( stream ) );
			}
			else
			{
				int skipByte;
				Encoding encoding = EncodingChecker.Check ( stream, out skipByte );
				if ( skipByte == 0 ) return ParseBinary ( new BinaryReader ( stream ) );
				else { stream.Position += skipByte; return ParseString ( new BinaryReader ( stream, encoding ) ); }
			}
		}
		public static JsonContainer Parse ( string str )
		{
			return Parse ( new MemoryStream ( Encoding.UTF8.GetBytes ( str ) ) );
		}

		private static JsonContainer BuildAndReturn ( ParseState parseMode, Queue<object> tokenStack )
		{
			JsonContainer container = new JsonContainer ( parseMode == ParseState.Object ? ContainType.Object : ContainType.Array );

			int index = 0;
			while ( tokenStack.Count > 0 )
			{
				object key;
				if ( parseMode == ParseState.Object )
					key = tokenStack.Dequeue ();
				else key = index++;
				object value = tokenStack.Dequeue ();
				container.Add ( value, key );
			}

			return container;
		}

		#region Get Data From String
		private static string GetStringFromString ( BinaryReader jsonString )
		{
			char ch;
			StringBuilder sb = new StringBuilder ();
			while ( ( ch = jsonString.ReadChar () ) != '"' || !( ch == '"' && sb [ sb.Length - 1 ] != '\\' ) )
				sb.Append ( ch );
			return sb.ToString ().Replace ( "\\n", "\n" ).Replace ( "\\r", "\r" ).Replace ( "\\\"", "\"" ).Replace ( "\\\\", "\\" );
		}

		private static bool GetBooleanFromString ( BinaryReader jsonString, char ch )
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ( ch );
			while ( true )
			{
				sb.Append ( jsonString.ReadChar () );
				if ( ch == 't' && sb.Length == 4 )
				{
					if ( sb.ToString () == "true" ) return true;
					else throw new Exception ( "Invalid JSON document." );
				}
				else if ( ch == 'f' && sb.Length == 5 )
				{
					if ( sb.ToString () == "false" ) return false;
					else throw new Exception ( "Invalid JSON document." );
				}
			}
		}

		private static object GetNullFromString ( BinaryReader jsonString )
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ( 'n' );
			while ( true )
			{
				sb.Append ( jsonString.ReadChar () );
				if ( sb.Length == 4 )
				{
					if ( sb.ToString () == "null" ) return true;
					else throw new Exception ( "Invalid JSON document." );
				}
			}
		}

		private static object GetNumberFromString ( BinaryReader jsonString, char ch )
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ( ch );

			bool isFloatingPoint = false;

			while ( true )
			{
				ch = jsonString.ReadChar ();
				if ( !( ( ch >= '0' && ch <= '9' ) || ch == '.' || ( isFloatingPoint && ( ch == 'e' || ch == 'E' || ch == '-' || ch == '+' ) ) ) )
				{
					jsonString.BaseStream.Position -= 1;
					break;
				}

				sb.Append ( ch );

				if ( ch == '.' && !isFloatingPoint ) isFloatingPoint = true;
				else if ( ch == '.' && isFloatingPoint ) throw new Exception ( "Invalid JSON document." );
			}

			if ( !isFloatingPoint )
			{
				int temp;
				if ( int.TryParse ( sb.ToString (), out temp ) ) return temp;
				else throw new Exception ( "Invalid JSON document." );
			}
			else 
			{
				double temp;
				if ( double.TryParse ( sb.ToString (), out temp ) ) return temp;
				else throw new Exception ( "Invalid JSON document." );
			}
		}
		#endregion

		#region Get Data From Binary
		private static string GetKeyFromBinary ( BinaryReader jsonBinary )
		{
			StringBuilder sb = new StringBuilder ();
			char ch;
			while ( ( ch = jsonBinary.ReadChar () ) != '\0' )
				sb.Append ( ch );
			return sb.ToString ();
		}

		private static string GetStringFromBinary ( BinaryReader jsonBinary )
		{
			int length = jsonBinary.ReadInt32 ();
			return Encoding.UTF8.GetString ( jsonBinary.ReadBytes ( length ), 0, length - 1 );
		}

		private static byte [] GetBinaryFromBinary ( BinaryReader jsonBinary )
		{
			int length = jsonBinary.ReadInt32 ();
			return jsonBinary.ReadBytes ( length );
		}
		#endregion
	}
}
