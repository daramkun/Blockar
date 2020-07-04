using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	partial class BlockarObject
	{
		#region Serialization
		/// <summary>
		/// INI 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="stream">직렬화한 데이터를 보관할 Stream 객체</param>
		/// <param name="objs">직렬화할 객체</param>
		public static void SerializeToIni (Stream stream, params BlockarObject [] objs)
		{
#if NET20 || NET35
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 4096, true))
#endif
				SerializeToIni (writer, objs);
		}

		/// <summary>
		/// INI 포맷으로 직렬화한 문자열을 가져온다.
		/// </summary>
		/// <returns>INI으로 직렬화한 문자열</returns>
		public string ToIniString ()
		{
			StringBuilder builder = new StringBuilder ();
			using (TextWriter writer = new StringWriter (builder))
				SerializeToIni (writer, this);
			return builder.ToString ();
		}

		/// <summary>
		/// INI 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="writer">직렬화한 데이터를 보관할 TextWriter 객체</param>
		/// <param name="objs">직렬화할 데이터</param>
		public static void SerializeToIni (TextWriter writer, params BlockarObject [] objs)
		{
			foreach (var obj in objs)
			{
				writer.WriteLine ($"[{obj.SectionName}]");
				foreach (var innerObj in obj.objs)
				{
					writer.Write (innerObj.Key);
					writer.Write ('=');
					__IniObjectToWriter (writer, innerObj.Value);
					writer.WriteLine ();
				}
				writer.WriteLine ();
				writer.Flush ();
			}
		}

		static void __IniObjectToWriter (TextWriter writer, object obj)
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
				__IniObjectToWriter (writer, obj.ToString ());
			// String
			else if (type == typeof (string))
			{
				var objStr = obj as string;
				if (__IniIsContainsInvalidStringCharacter (objStr))
				{
					var builder = new StringBuilder ("\"");
					foreach (var ch in (string) obj)
					{
						if (ch <= 127 && !char.IsControl (ch))
						{
							if (ch == '\\' || ch == '"')
								builder.Append ('\\');
							builder.Append (ch);
						}
						else switch (ch)
						{
							case '\t':
								builder.Append ("\\t");
								break;
							case '\n':
								builder.Append ("\\n");
								break;
							case '\r':
								builder.Append ("\\r");
								break;
							default:
								builder.Append ("\\u");
								builder.Append ($"{(int)ch:X}".PadLeft (4, '0'));
								break;
						}
					}
					writer.Write (builder.Append ("\"").ToString ());
				}
				else
					writer.Write (objStr);
			}
		}

		static bool __IniIsContainsInvalidStringCharacter (string str)
		{
			foreach (char ch in str)
			{
				if (ch == '\r' || ch == '\n' || ch == ';')
					return true;
			}
			return false;
		}
		#endregion

		#region Deserialization

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="stream">JSON 데이터가 보관된 Stream 객체</param>
		/// <param name="sectionName">섹션 이름</param>
		public static BlockarObject DeserializeFromIni (Stream stream, string sectionName)
		{
#if NET20 || NET35
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true);
#else
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true);
#endif
			return DeserializeFromIni (reader, sectionName);
		}

		public static IEnumerable<BlockarObject> DeserializeFromIni (Stream stream)
		{
#if NET20 || NET35
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true);
#else
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true);
#endif
			return DeserializeFromIni (reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="ini">JSON 문자열</param>
		/// <param name="sectionName">섹션 이름</param>
		public static BlockarObject DeserializeFromIni (string ini, string sectionName)
		{
			TextReader reader = new StringReader (ini);
			return DeserializeFromIni (reader, sectionName);
		}

		public static IEnumerable<BlockarObject> DeserializeFromIni (string ini)
		{
			TextReader reader = new StringReader (ini);
			return DeserializeFromIni (reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="reader">JSON 데이터를 읽어올 수 있는 TextReader 객체</param>
		/// <param name="sectionName">섹션 이름</param>
		public static BlockarObject DeserializeFromIni (TextReader reader, string sectionName)
		{
			return DeserializeFromIni(reader).FirstOrDefault(obj => sectionName == null || sectionName == obj.SectionName);
		}

		public static IEnumerable<BlockarObject> DeserializeFromIni (TextReader reader)
		{
			BlockarObject obj = new BlockarObject ();

			while (true)
			{
				int i = 0;
				string line = reader.ReadLine ();
				if (line == null)
					break;
				if (line.Length == 0)
					continue;
				for (; i < line.Length; ++i)
				{
					char ch = line [i];
					if (ch != ' ' && ch != '\t' && ch != '\a' && ch != '\r')
						break;
				}
				switch (line [i])
				{
					case ';':
						continue;
					case '[':
						{
							if (obj.Count > 0)
								yield return obj;
							obj = new BlockarObject {SectionName = __IniGetSectionTitle(line, i + 1)};
							break;
						}
					default:
						{
							var key = __IniGetKey (line, ref i);
							var value = __IniGetValue (line, i);
							obj.Set (key, value);
							break;
						}
				}
			}
			yield return obj;
		}

		static string __IniGetSectionTitle (string line, int startIndex)
		{
			StringBuilder sb = new StringBuilder ();
			for (; startIndex < line.Length && line [startIndex] != ']'; ++startIndex)
				sb.Append (line [startIndex]);
			return sb.ToString ();
		}

		static string __IniGetKey (string line, ref int startIndex)
		{
			StringBuilder sb = new StringBuilder ();
			for (; startIndex < line.Length && line [startIndex] != '='; ++startIndex)
				sb.Append (line [startIndex]);
			++startIndex;
			return sb.ToString ().Trim ();
		}

		static string __IniGetValue (string line, int startIndex)
		{
			if (line.Length == startIndex) return "";

			var sb = new StringBuilder ();
			for (; startIndex < line.Length; ++startIndex)
			{
				var ch = line [startIndex];
				if (ch != ' ' && ch != '	' && ch != '\a' && ch != '\r')
					break;
			}
			if (line [startIndex] == '"')
			{
				++startIndex;
				for (; startIndex < line.Length && line [startIndex] != '"'; ++startIndex)
					sb.Append (line [startIndex]);
			}
			else
			{
				for (; startIndex < line.Length && (line [startIndex] != '\n' && line [startIndex] != ';'); ++startIndex)
					sb.Append (line [startIndex]);
			}
			return sb.ToString ().Trim ();
		}
		#endregion
	}
}
