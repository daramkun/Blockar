using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
#if !NET20
using System.Linq;
#endif
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	partial class BlockarObject
	{
#region Serialization
		/// <summary>
		/// JSON 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="stream">직렬화한 데이터를 보관할 Stream 객체</param>
		public static void SerializeToJson (Stream stream, BlockarObject obj)
		{
#if NET20 || NET35
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 4096, true))
#endif
			{
				SerializeToJson (writer, obj);
			}
		}

		/// <summary>
		/// JSON 포맷으로 직렬화한 문자열을 가져온다.
		/// </summary>
		/// <returns>JSON으로 직렬화한 문자열</returns>
		public string ToJsonString ()
		{
			StringBuilder builder = new StringBuilder ();
			using (TextWriter writer = new StringWriter (builder))
				SerializeToJson (writer, this);
			return builder.ToString ();
		}

		/// <summary>
		/// JSON 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="writer">직렬화한 데이터를 보관할 TextWriter 객체</param>
		public static void SerializeToJson (TextWriter writer, BlockarObject obj)
		{
			writer.Write ('{');
			foreach (var innerObj in obj.objs)
			{
				writer.Write ($"\"{innerObj.Key}\":");
				__JsonObjectToWriter (writer, innerObj.Value);

				if (innerObj != obj.objs [obj.objs.Count - 1])
					writer.Write (',');
			}
			writer.Write ('}');
			writer.Flush ();
		}

		static void __JsonObjectToWriter (TextWriter writer, object obj)
		{
			Type type = obj.GetType ();
			// Integers
			if (type == typeof (byte) || type == typeof (sbyte) || type == typeof (short)
				|| type == typeof (ushort) || type == typeof (int) || type == typeof (uint)
					|| type == typeof (long) || type == typeof (ulong) || type == typeof (IntPtr))
				writer.Write (obj.ToString ());
			// Floating Points
			else if (type == typeof (float) || type == typeof (double) || type == typeof (decimal))
				writer.Write (obj.ToString ());
			// Boolean
			else if (type == typeof (bool))
				writer.Write (obj.Equals (true) ? "true" : "false");
			// DateTime
			else if (type == typeof (DateTime))
				writer.Write ($"\"{obj:yyyy-MM-ddTHH:mm:ssZ}\"");
			// TimeStamp via Nanosecs
			else if (type == typeof (TimeSpan))
				writer.Write ($"\"{obj:Thh:mm:ssZ}\"");
			// Regular Expression to String
			else if (type == typeof (Regex))
				__JsonObjectToWriter (writer, obj.ToString ());
			// String
			else if (type == typeof (string))
			{
				StringBuilder builder = new StringBuilder ("\"");
				foreach (char ch in obj as string)
				{
					if (ch <= 127 && !char.IsControl (ch))
					{
						if (ch == '\\' || ch == '"')
							builder.Append ('\\');
						builder.Append (ch);
					}
					else if (ch == '\t')
						builder.Append ("\\t");
					else if (ch == '\n')
						builder.Append ("\\n");
					else if (ch == '\r')
						builder.Append ("\\r");
					else
					{
						builder.Append ("\\u");
						builder.Append ($"{((int) ch):X}".PadLeft (4, '0'));
					}
				}
				writer.Write (builder.Append ("\"").ToString ());
			}
			// BlockarObject compatible Dictionary
			else if (obj is IDictionary<string, object>)
			{
				SerializeToJson (writer, FromDictionary (obj as IDictionary<string, object>));
			}
			// BlockarObject not compatible Dictionary
			else if (type.GetInterface ("IDictionary") != null)
			{
				writer.Write ('{');
				bool isFirst = true;
				var dict = obj as IDictionary;
				foreach (var key in dict.Keys)
				{
					if (!isFirst)
						writer.Write (',');
					var value = dict [key];
					writer.Write ($"\"{key}\":");
					__JsonObjectToWriter (writer, value);
				}
				writer.Write ('}');
			}
			// Array, List, ...
			else if (type.IsArray || type.GetInterface ("IEnumerable") != null)
			{
				writer.Write ('[');
				bool isFirst = true;
				foreach (var e in obj as IEnumerable)
				{
					if (!isFirst)
						writer.Write (',');
					__JsonObjectToWriter (writer, e);
					isFirst = false;
				}
				writer.Write (']');
			}
			else if (obj is BlockarObject)
			{
				SerializeToJson (writer , obj as BlockarObject);
			}
			// Any Object
			else
			{
				SerializeToJson (writer, FromObject (type, obj));
			}
		}
#endregion

#region Deserialization
		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="stream">JSON 데이터가 보관된 Stream 객체</param>
		public static BlockarObject DeserializeFromJson (Stream stream)
		{
#if NET20 || NET35
			using (TextReader reader = new StreamReader (stream, Encoding.UTF8, true))
#else
			using (TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true))
#endif
				return DeserializeFromJson (reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="json">JSON 문자열</param>
		public static BlockarObject DeserializeFromJson (string json)
		{
			using (TextReader reader = new StringReader (json))
				return DeserializeFromJson (reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="reader">JSON 데이터를 읽어올 수 있는 TextReader 객체</param>
		public static BlockarObject DeserializeFromJson (TextReader reader)
		{
			BlockarObject obj = new BlockarObject ();
			char rc = __JsonPassWhitespaces (reader);
			if (rc == '{')
				__JsonInnerDeserializeObjectFromJson (obj, reader);
			else
				throw new Exception ("Invalid JSON Object.");
			return obj;
		}

		enum JsonParseState
		{
			None,

			Key,
			Value,
		}

		static readonly char [] JSON_TRUE_ARRAY = new char [] { 't', 'r', 'u', 'e' };
		static readonly char [] JSON_FALSE_ARRAY = new char [] { 'f', 'a', 'l', 's', 'e' };
		static readonly char [] JSON_NULL_ARRAY = new char [] { 'n', 'u', 'l', 'l' };

		static char __JsonPassWhitespaces(TextReader reader)
		{
			char rc = '\0';
			do
			{
				rc = (char) reader.Read ();
			} while (rc == ' ' || rc == '	' || rc == '　' || rc == '\n' || rc == '\r');
			return rc;
		}

		static string __JsonGetStringFromString (TextReader reader)
		{
			char ToCharFromHexa (char [] p, int length)
			{
#if NET20
				StringBuilder builder = new StringBuilder ();
				for (int i = 0; i < length; ++i)
					builder.Append (p [i]);
				return Convert.ToChar (ushort.Parse (builder.ToString (), NumberStyles.AllowHexSpecifier));
#else
				return Convert.ToChar (ushort.Parse (string.Concat (p.Take (length)), NumberStyles.AllowHexSpecifier));
#endif
			}

			StringBuilder sb = new StringBuilder ();
			bool backslashMode = false;
			char [] charBuffer = new char [4];
			while (true)
			{
				char ch = (char) reader.Read ();
				if (backslashMode)
				{
					switch (ch)
					{
						case 'n': sb [sb.Length - 1] = '\n'; break;
						case 'r': sb [sb.Length - 1] = '\r'; break;
						case 't': sb [sb.Length - 1] = '\t'; break;
						case '\\': sb [sb.Length - 1] = '\\'; break;
						case '"': sb [sb.Length - 1] = '"'; break;
						case '/': sb [sb.Length - 1] = '/'; break;
						case 'b': sb [sb.Length - 1] = '\b'; break;
						case 'f': sb [sb.Length - 1] = '\f'; break;
						case 'u':
							sb.Remove (sb.Length - 1, 1);
							reader.Read (charBuffer, 0, 4);
							sb.Append (ToCharFromHexa (charBuffer, 4));
							break;
						case 'x':
							sb.Remove (sb.Length - 1, 1);
							reader.Read (charBuffer, 0, 2);
							sb.Append (ToCharFromHexa (charBuffer, 2));
							break;

						default: throw new Exception ();
					}
					backslashMode = false;
				}
				else if (ch == '"')
					break;
				else
				{
					sb.Append (ch);
					if (ch == '\\')
						backslashMode = true;
				}
			}
			return sb.ToString ();
		}

		static object __JsonGetKeywordFromString (TextReader reader, char ch)
		{
			bool CheckArray (char [] v1, char [] v2, int length)
			{
				for (int i = 0; i < length; ++i)
				{
					if (v1 [i] != v2 [i])
						return false;
				}
				return true;
			}

			char [] charBuffer = new char [5];
			charBuffer [0] = ch;
			switch (ch)
			{
				case 't':   // true
					reader.Read (charBuffer, 1, 3);
					if (CheckArray (charBuffer, JSON_TRUE_ARRAY, 4)) return true;
					goto default;
				case 'f':   // false
					reader.Read (charBuffer, 1, 4);
					if (CheckArray (charBuffer, JSON_FALSE_ARRAY, 5)) return false;
					goto default;
				case 'n':   // null
					reader.Read (charBuffer, 1, 3);
					if (CheckArray (charBuffer, JSON_NULL_ARRAY, 4)) return null;
					goto default;
				default:
					throw new Exception ("Invalid JSON document.");
			}
		}

		static object __JsonGetNumberFromString (TextReader reader, char ch)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (ch);

			bool isFloatingPoint = false;

			while (true)
			{
				ch = (char) reader.Peek ();
				if (!((ch >= '0' && ch <= '9') || ch == '.' || (isFloatingPoint && (ch == 'e' || ch == 'E' || ch == '-' || ch == '+'))))
					break;
				reader.Read ();

				sb.Append (ch);

				if (ch == '.' && !isFloatingPoint) isFloatingPoint = true;
				else if (ch == '.' && isFloatingPoint) throw new Exception ("Invalid JSON document.");
			}

			if (!isFloatingPoint)
			{
				int temp;
				if (int.TryParse (sb.ToString (), out temp)) return temp;
				else throw new Exception ("Invalid JSON document.");
			}
			else
			{
				double temp;
				if (double.TryParse (sb.ToString (), out temp)) return temp;
				else throw new Exception ("Invalid JSON document.");
			}
		}

		static void __JsonInnerDeserializeObjectFromJson (BlockarObject blockarObject, TextReader reader)
		{
			try
			{
				JsonParseState parseState = JsonParseState.Key;
				Queue<object> tokenStack = new Queue<object> ();

				while (true)
				{
					char rc = __JsonPassWhitespaces (reader);
					if (rc == ':')
					{
						if (parseState != JsonParseState.Key)
							throw new Exception ("Invalid JSON document: When the Parse mode is Object, You must have key.");
						parseState = JsonParseState.Value;
					}
					else if (rc == ',')
					{
						if (parseState != JsonParseState.Value)
							throw new Exception ("Invalid JSON document.");
						parseState = JsonParseState.Key;
					}
					else if (rc == '"')
						tokenStack.Enqueue (__JsonGetStringFromString (reader));
					else if (rc == 't' || rc == 'f' || rc == 'n')
						tokenStack.Enqueue (__JsonGetKeywordFromString (reader, rc));
					else if ((rc >= '0' && rc <= '9') || rc == '-' || rc == '+')
						tokenStack.Enqueue (__JsonGetNumberFromString (reader, rc));
					else if (rc == '{')
					{
						BlockarObject inner = new BlockarObject ();
						__JsonInnerDeserializeObjectFromJson (inner, reader);
						tokenStack.Enqueue (inner);
					}
					else if (rc == '[')
					{
						List<object> inner = new List<object> ();
						InnerDeserializeArrayFromJson (inner, reader);
						tokenStack.Enqueue (inner);
					}
					else if (rc == '}')
					{
						break;
					}
					else
						throw new ArgumentException ("Invalid JSON document.");
				}

				if (tokenStack.Count % 2 != 0)
					throw new ArgumentException ("Invalid JSON document.");

				while(tokenStack.Count != 0)
				{
					string key = tokenStack.Dequeue () as string;
					object value = tokenStack.Dequeue ();
					blockarObject.Set (key, value);
				}
			}
			catch (Exception ex) { throw new ArgumentException ("Invalid JSON document.", ex); }
		}

		static void InnerDeserializeArrayFromJson (List<object> arr, TextReader reader)
		{
			try
			{
				JsonParseState parseState = JsonParseState.None;

				while (true)
				{
					char rc = __JsonPassWhitespaces (reader);
					if (rc == ',')
					{
						parseState = JsonParseState.None;
					}
					else if (rc == '"')
					{
						if (parseState != JsonParseState.None)
							throw new Exception ("Invalid JSON Document.");
						arr.Add (__JsonGetStringFromString (reader));
						parseState = JsonParseState.Value;
					}
					else if (rc == 't' || rc == 'f' || rc == 'n')
					{
						if (parseState != JsonParseState.None)
							throw new Exception ("Invalid JSON Document.");
						arr.Add (__JsonGetKeywordFromString (reader, rc));
						parseState = JsonParseState.Value;
					}
					else if ((rc >= '0' && rc <= '9') || rc == '-' || rc == '+')
					{
						if (parseState != JsonParseState.None)
							throw new Exception ("Invalid JSON Document.");
						arr.Add (__JsonGetNumberFromString (reader, rc));
						parseState = JsonParseState.Value;
					}
					else if (rc == '{')
					{
						if (parseState != JsonParseState.None)
							throw new Exception ("Invalid JSON Document.");
						BlockarObject inner = new BlockarObject ();
						__JsonInnerDeserializeObjectFromJson (inner, reader);
						arr.Add (inner);
						parseState = JsonParseState.Value;
					}
					else if (rc == '[')
					{
						if (parseState != JsonParseState.None)
							throw new Exception ("Invalid JSON Document.");
						List<object> inner = new List<object> ();
						InnerDeserializeArrayFromJson (inner, reader);
						arr.Add (inner);
						parseState = JsonParseState.Value;
					}
					else if (rc == ']')
					{
						break;
					}
					else
						throw new ArgumentException ("Invalid JSON document.");
				}
			}
			catch (Exception ex) { throw new ArgumentException ("Invalid JSON document.", ex); }
		}
#endregion
	}
}
