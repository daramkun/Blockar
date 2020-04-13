using System;
using System.Collections.Generic;
using System.IO;
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
		public static void SerializeToIni (BlockarObject obj, Stream stream)
		{
#if NET20
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 4096, true))
#endif
				SerializeToIni (obj, writer);
		}

		/// <summary>
		/// INI 포맷으로 직렬화한 문자열을 가져온다.
		/// </summary>
		/// <returns>INI으로 직렬화한 문자열</returns>
		public string ToIniString ()
		{
			StringBuilder builder = new StringBuilder ();
			using (TextWriter writer = new StringWriter (builder))
				SerializeToIni (this, writer);
			return builder.ToString ();
		}

		/// <summary>
		/// INI 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="writer">직렬화한 데이터를 보관할 TextWriter 객체</param>
		public static void SerializeToIni (BlockarObject obj, TextWriter writer)
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

		static void __IniObjectToWriter (TextWriter writer, object obj)
		{
			Type type = obj.GetType ();
			// Integers
			if (type == typeof (byte) || type == typeof (sbyte) || type == typeof (short)
				|| type == typeof (ushort) || type == typeof (int) || type == typeof (uint)
					|| type == typeof (long) || type == typeof (ulong))
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
				string objStr = obj as string;
				if (__IniIsContainsInvalidStringCharacter (objStr))
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
							builder.Append ($"{ch:X}".PadLeft (4, '0'));
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
			foreach(char ch in str)
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
		public static void DeserializeFromIni (BlockarObject obj, Stream stream, string sectionName)
		{
#if NET20
			using (TextReader reader = new StreamReader (stream, Encoding.UTF8, true))
#else
			using (TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true))
#endif
				DeserializeFromIni (obj, reader, sectionName);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="json">JSON 문자열</param>
		public static void DeserializeFromIni (BlockarObject obj, string json, string sectionName)
		{
			using (TextReader reader = new StringReader (json))
				DeserializeFromIni (obj, reader, sectionName);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="reader">JSON 데이터를 읽어올 수 있는 TextReader 객체</param>
		public static void DeserializeFromIni (BlockarObject obj, TextReader reader, string sectionName)
		{
			obj.Clear ();

			bool skipSection = true;
			while (true)
			{
				int i = 0;
				string line = reader.ReadLine ();
				if (line.Length == 0)
					continue;
				for (; i < line.Length; ++i)
				{
					char ch = line [i];
					if (ch != ' ' && ch != '\t' && ch != '\a' && ch != '\r')
						break;
				}
				if (line [i] == ';') continue;
				else if (line [i] == '[')
				{
					if (sectionName == __IniGetSectionTitle (line, i + 1) || sectionName == null)
						skipSection = false;
					else
						skipSection = true;
				}
				else
				{
					string key = __IniGetKey (line, ref i);
					string value = __IniGetValue (line, i);
					if (!skipSection)
						obj.Set (key, value);
				}
			}
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

			StringBuilder sb = new StringBuilder ();
			for (; startIndex < line.Length; ++startIndex)
			{
				char ch = line [startIndex];
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
