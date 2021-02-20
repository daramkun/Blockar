using System;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
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
		public static void SerializeToJson(Stream stream, BlockarObject obj)
		{
#if NET20 || NET35
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 4096, true))
#endif
			{
				SerializeToJson(writer, obj);
			}
		}

		/// <summary>
		/// JSON 포맷으로 직렬화한 문자열을 가져온다.
		/// </summary>
		/// <returns>JSON으로 직렬화한 문자열</returns>
		public string ToJsonString()
		{
			StringBuilder builder = new StringBuilder();
			using (TextWriter writer = new StringWriter(builder))
				SerializeToJson(writer, this);
			return builder.ToString();
		}

		/// <summary>
		/// JSON 포맷으로 직렬화한다.
		/// </summary>
		/// <param name="writer">직렬화한 데이터를 보관할 TextWriter 객체</param>
		/// <param name="obj">직렬화할 데이터</param>
		public static void SerializeToJson(TextWriter writer, BlockarObject obj)
		{
			writer.Write('{');
			foreach (var innerObj in obj.objs)
			{
				writer.Write($"\"{innerObj.Key}\":");
				__JsonObjectToWriter(writer, innerObj.Value);

				if (innerObj != obj.objs[obj.objs.Count - 1])
					writer.Write(',');
			}

			writer.Write('}');
			writer.Flush();
		}

		static void __JsonObjectToWriter(TextWriter writer, object obj)
		{
			if (obj == null)
			{
				writer.Write("null");
				return;
			}

			Type type = obj.GetType();
			// Integers
			if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short)
			    || type == typeof(ushort) || type == typeof(int) || type == typeof(uint)
			    || type == typeof(long) || type == typeof(ulong) || type == typeof(IntPtr))
				writer.Write(obj.ToString());
			else if (type.IsSubclassOf(typeof(Enum)) || type == typeof(Enum))
				writer.Write($"\"{obj}\"");
			// Floating Points
			else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
				writer.Write(obj.ToString());
			// Boolean
			else if (type == typeof(bool))
				writer.Write(obj.Equals(true) ? "true" : "false");
			// DateTime
			else if (type == typeof(DateTime))
				writer.Write($"\"{obj:yyyy-MM-ddTHH:mm:ssZ}\"");
			// TimeStamp via Nanosecs
			else if (type == typeof(TimeSpan))
				writer.Write($"\"{obj:Thh:mm:ssZ}\"");
			// Regular Expression to String
			else if (type == typeof(Regex))
				__JsonObjectToWriter(writer, obj.ToString());
			// String
			else if (type == typeof(string))
			{
				var builder = new StringBuilder("\"");
				foreach (var ch in (string) obj)
				{
					if (ch <= 127 && !char.IsControl(ch))
					{
						if (ch == '\\' || ch == '"')
							builder.Append('\\');
						builder.Append(ch);
					}
					else switch (ch)
					{
						case '\t':
							builder.Append("\\t");
							break;
						case '\n':
							builder.Append("\\n");
							break;
						case '\r':
							builder.Append("\\r");
							break;
						default:
							builder.Append("\\u");
							builder.Append($"{((int) ch):X}".PadLeft(4, '0'));
							break;
					}
				}

				writer.Write(builder.Append("\"").ToString());
			}
			else if (obj is BlockarObject blockarObject)
			{
				SerializeToJson(writer, blockarObject);
			}
			// BlockarObject compatible Dictionary
			else if (obj is IDictionary<string, object> dictionary)
			{
				SerializeToJson(writer, FromDictionary(dictionary));
			}
			// BlockarObject not compatible Dictionary
			else if (type.GetInterface("IDictionary") != null)
			{
				writer.Write('{');
				var isFirst = true;
				var dict = obj as IDictionary;
				foreach (var key in dict.Keys)
				{
					if (!isFirst)
						writer.Write(',');
					isFirst = false;
					var value = dict[key];
					writer.Write($"\"{key}\":");
					__JsonObjectToWriter(writer, value);
				}

				writer.Write('}');
			}
			// Array, List, ...
			else if (type.IsArray || type.GetInterface("IEnumerable") != null)
			{
				writer.Write('[');
				var isFirst = true;
				foreach (var e in (IEnumerable) obj)
				{
					if (!isFirst)
						writer.Write(',');
					isFirst = false;
					__JsonObjectToWriter(writer, e);
				}

				writer.Write(']');
			}
			// Any Object
			else
			{
				SerializeToJson(writer, FromObject(type, obj));
			}
		}

		#endregion

		#region Deserialization

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="stream">JSON 데이터가 보관된 Stream 객체</param>
		public static BlockarObject DeserializeFromJson(Stream stream)
		{
#if NET20 || NET35
			using (TextReader reader = new StreamReader (stream, Encoding.UTF8, true))
#else
			using (TextReader reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
#endif
				return DeserializeFromJson(reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="json">JSON 문자열</param>
		public static BlockarObject DeserializeFromJson(string json)
		{
			using (TextReader reader = new StringReader(json))
				return DeserializeFromJson(reader);
		}

		/// <summary>
		/// JSON 포맷에서 직렬화를 해제한다.
		/// </summary>
		/// <param name="reader">JSON 데이터를 읽어올 수 있는 TextReader 객체</param>
		public static BlockarObject DeserializeFromJson(TextReader reader)
		{
			BlockarObject obj = new BlockarObject();
			char rc = __JsonPassWhitespaces(reader);
			if (rc == 0xffff)
				return obj;

			if (rc == '{')
			{
				StringBuilder stringBuilder = new StringBuilder(4096);
				char[] charBuffer = new char [6];
				__JsonInnerDeserializeObjectFromJson(obj, reader, stringBuilder, charBuffer);
			}
			else
				throw new BlockarJsonDeserializeException();

			return obj;
		}

		enum JsonParseState
		{
			None,

			Key,
			Value,
		}

		static readonly char[] JSON_TRUE_ARRAY = new char[] {'t', 'r', 'u', 'e'};
		static readonly char[] JSON_FALSE_ARRAY = new char[] {'f', 'a', 'l', 's', 'e'};
		static readonly char[] JSON_NULL_ARRAY = new char[] {'n', 'u', 'l', 'l'};

		static char __JsonPassWhitespaces(TextReader reader)
		{
			var rc = '\0';
			do
			{
				rc = (char) reader.Read();
			} while (rc == ' ' || rc == '	' || rc == '　' || rc == '\n' || rc == '\r');

			return rc;
		}

		static void __JsonGetStringFromString(TextReader reader, StringBuilder builder, char[] charBuffer)
		{
			char ToCharFromHexa(char[] p, int length)
			{
				ushort hexInt = 0;
				for (int i = 0; i < length; ++i)
					hexInt = (ushort) ((hexInt * 10) + (ushort) (p[i] - '0'));
				return Convert.ToChar(hexInt);
			}

			bool backslashMode = false;
			while (true)
			{
				char ch = (char) reader.Read();
				if (backslashMode)
				{
					switch (ch)
					{
						case 'n':
							builder[builder.Length - 1] = '\n';
							break;
						case 'r':
							builder[builder.Length - 1] = '\r';
							break;
						case 't':
							builder[builder.Length - 1] = '\t';
							break;
						case '\\':
							builder[builder.Length - 1] = '\\';
							break;
						case '"':
							builder[builder.Length - 1] = '"';
							break;
						case '/':
							builder[builder.Length - 1] = '/';
							break;
						case 'b':
							builder[builder.Length - 1] = '\b';
							break;
						case 'f':
							builder[builder.Length - 1] = '\f';
							break;
						case 'u':
							{
								builder.Remove(builder.Length - 1, 1);
								reader.Read(charBuffer, 0, 4);
								builder.Append(ToCharFromHexa(charBuffer, 4));
							}
							break;
						case 'x':
							{
								builder.Remove(builder.Length - 1, 1);
								reader.Read(charBuffer, 0, 2);
								builder.Append(ToCharFromHexa(charBuffer, 2));
							}
							break;

						default: throw new Exception();
					}

					backslashMode = false;
				}
				else if (ch == '"')
					break;
				else
				{
					builder.Append(ch);
					if (ch == '\\')
						backslashMode = true;
				}
			}
		}

		static object __JsonGetKeywordFromString(TextReader reader, char ch, char[] charBuffer)
		{
			bool CheckArray(char[] v1, char[] v2, int length)
			{
				for (int i = 0; i < length; ++i)
				{
					if (v1[i] != v2[i])
						return false;
				}

				return true;
			}

			charBuffer[0] = ch;
			switch (ch)
			{
				case 't': // true
					reader.Read(charBuffer, 1, 3);
					if (CheckArray(charBuffer, JSON_TRUE_ARRAY, 4)) return true;
					goto default;
				case 'f': // false
					reader.Read(charBuffer, 1, 4);
					if (CheckArray(charBuffer, JSON_FALSE_ARRAY, 5)) return false;
					goto default;
				case 'n': // null
					reader.Read(charBuffer, 1, 3);
					if (CheckArray(charBuffer, JSON_NULL_ARRAY, 4)) return null;
					goto default;
				default:
					throw new BlockarJsonDeserializeException();
			}
		}

		static object __JsonGetNumberFromString(TextReader reader, char ch, StringBuilder builder)
		{
			builder.Append(ch);

			bool isFloatingPoint = false;

			while (true)
			{
				ch = (char) reader.Peek();
				if (!((ch >= '0' && ch <= '9') || ch == '.' ||
				      (isFloatingPoint && (ch == 'e' || ch == 'E' || ch == '-' || ch == '+'))))
					break;
				reader.Read();

				builder.Append(ch);

				if (ch == '.' && !isFloatingPoint) isFloatingPoint = true;
				else if (ch == '.' && isFloatingPoint) throw new BlockarJsonDeserializeException();
			}

			if (!isFloatingPoint)
			{
				int temp;
				if (int.TryParse(builder.ToString(), out temp)) return temp;
				else throw new BlockarJsonDeserializeException();
			}
			else
			{
				double temp;
				if (double.TryParse(builder.ToString(), out temp)) return temp;
				else throw new BlockarJsonDeserializeException();
			}
		}

#if !NET35
		private static readonly ConcurrentQueue<Queue<object>> tokenStackQueue = new ConcurrentQueue<Queue<object>>();
#endif

		static void __JsonInnerDeserializeObjectFromJson(BlockarObject blockarObject, TextReader reader,
			StringBuilder stringBuilder, char[] charBuffer)
		{
			try
			{
				var parseState = JsonParseState.Key;

#if !NET35
				if (!tokenStackQueue.TryDequeue(out var tokenStack))
					tokenStack = new Queue<object>(128);
#else
				var tokenStack = new Queue<object> (128);
#endif

				while (true)
				{
#if !(NET20 || NET35)
					stringBuilder.Clear();
#else
					stringBuilder.Remove (0, stringBuilder.Length);
#endif

					var rc = __JsonPassWhitespaces(reader);
					if (rc == ':')
					{
						if (parseState != JsonParseState.Key)
							throw new BlockarJsonDeserializeException();
						parseState = JsonParseState.Value;
					}
					else if (rc == ',')
					{
						if (parseState != JsonParseState.Value)
							throw new BlockarJsonDeserializeException();
						parseState = JsonParseState.Key;
					}
					else if (rc == '"')
					{
						__JsonGetStringFromString(reader, stringBuilder, charBuffer);
						tokenStack.Enqueue(stringBuilder.ToString());
					}
					else if (rc == 't' || rc == 'f' || rc == 'n')
						tokenStack.Enqueue(__JsonGetKeywordFromString(reader, rc, charBuffer));
					else if ((rc >= '0' && rc <= '9') || rc == '-' || rc == '+')
						tokenStack.Enqueue(__JsonGetNumberFromString(reader, rc, stringBuilder));
					else if (rc == '{')
					{
						var inner = new BlockarObject();
						__JsonInnerDeserializeObjectFromJson(inner, reader, stringBuilder, charBuffer);
						tokenStack.Enqueue(inner);
					}
					else if (rc == '[')
					{
						var inner = new List<object>(16);
						InnerDeserializeArrayFromJson(inner, reader, stringBuilder, charBuffer);
						tokenStack.Enqueue(inner);
					}
					else if (rc == '}')
					{
						break;
					}
					else
						throw new BlockarJsonDeserializeException();
				}

				if (tokenStack.Count % 2 != 0)
					throw new BlockarJsonDeserializeException();

				while (tokenStack.Count != 0)
				{
					string key = tokenStack.Dequeue() as string;
					object value = tokenStack.Dequeue();
					blockarObject.Set(key, value);
				}

#if !NET35
				tokenStackQueue.Enqueue(tokenStack);
#endif
			}
			catch (Exception ex)
			{
				throw new BlockarJsonDeserializeException(ex);
			}
		}

		static void InnerDeserializeArrayFromJson(List<object> arr, TextReader reader, StringBuilder stringBuilder,
			char[] charBuffer)
		{
			try
			{
				JsonParseState parseState = JsonParseState.None;

				while (true)
				{
#if !(NET20 || NET35)
					stringBuilder.Clear();
#else
					stringBuilder.Remove (0, stringBuilder.Length);
#endif

					char rc = __JsonPassWhitespaces(reader);
					if (rc == ',')
					{
						parseState = JsonParseState.None;
					}
					else if (rc == '"')
					{
						if (parseState != JsonParseState.None)
							throw new BlockarJsonDeserializeException();
						__JsonGetStringFromString(reader, stringBuilder, charBuffer);
						if (arr.Count == arr.Capacity)
							arr.Capacity *= 2;
						arr.Add(stringBuilder.ToString());
						parseState = JsonParseState.Value;
					}
					else if (rc == 't' || rc == 'f' || rc == 'n')
					{
						if (parseState != JsonParseState.None)
							throw new BlockarJsonDeserializeException();
						if (arr.Count == arr.Capacity)
							arr.Capacity *= 2;
						arr.Add(__JsonGetKeywordFromString(reader, rc, charBuffer));
						parseState = JsonParseState.Value;
					}
					else if ((rc >= '0' && rc <= '9') || rc == '-' || rc == '+')
					{
						if (parseState != JsonParseState.None)
							throw new BlockarJsonDeserializeException();
						if (arr.Count == arr.Capacity)
							arr.Capacity *= 2;
						arr.Add(__JsonGetNumberFromString(reader, rc, stringBuilder));
						parseState = JsonParseState.Value;
					}
					else if (rc == '{')
					{
						if (parseState != JsonParseState.None)
							throw new BlockarJsonDeserializeException();
						BlockarObject inner = new BlockarObject();
						__JsonInnerDeserializeObjectFromJson(inner, reader, stringBuilder, charBuffer);
						if (arr.Count == arr.Capacity)
							arr.Capacity *= 2;
						arr.Add(inner);
						parseState = JsonParseState.Value;
					}
					else if (rc == '[')
					{
						if (parseState != JsonParseState.None)
							throw new BlockarJsonDeserializeException();
						List<object> inner = new List<object>();
						InnerDeserializeArrayFromJson(inner, reader, stringBuilder, charBuffer);
						if (arr.Count == arr.Capacity)
							arr.Capacity *= 2;
						arr.Add(inner);
						parseState = JsonParseState.Value;
					}
					else if (rc == ']')
					{
						break;
					}
					else
						throw new BlockarJsonDeserializeException();
				}
			}
			catch (Exception ex)
			{
				throw new BlockarJsonDeserializeException(ex);
			}
		}

		#endregion
	}
}
