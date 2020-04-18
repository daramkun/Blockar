using System;
using System.Collections.Generic;
using System.IO;
#if !NET20
using System.Linq;
#endif
using System.Text;

namespace Daramee.Blockar
{
	partial class BlockarObject
	{
		public const char CsvSeparatorDetectorCharacter = (char) 0xffff;

#region Serialization
		public static void SerializeToCsv (Stream stream, char separator = ',', params BlockarObject [] objs)
		{
#if NET20
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 4096, true))
#endif
			{
				SerializeToCsv (writer, separator, objs);
			}
		}

		public static string SafeText (string text, char separator, bool isForColumnName = false)
		{
			if (isForColumnName
#if NET20
				&& (text.IndexOf (separator) >= 0 || text.IndexOf (',') >= 0 || text.IndexOf ('|') >= 0 || text.IndexOf ('\t') >= 0 || text.IndexOf ('"') >= 0 || text.IndexOf ('\n') >= 0))
#else
				&& (text.Contains (separator) || text.Contains (',') || text.Contains ('|') || text.Contains ('\t') || text.Contains ('"') || text.Contains ('\n')))
#endif
				throw new ArgumentException ($"Column Name cannot contains ',', '|', '\\t', '\"', '\\n' and ${separator}.");

#if NET20
			if (text.IndexOf (separator) >= 0 || text.IndexOf ('"') >= 0 || text.IndexOf ('\n') >= 0)
#else
			if (text.Contains (separator) || text.Contains ('"') || text.Contains ('\n'))
#endif
			{
				return $"\"{text}\"";
			}
			return text;
		}

		public static void SerializeToCsv (TextWriter writer, char separator = ',', params BlockarObject [] objs)
		{
			List<string> columnNames = new List<string> ();
			foreach (var obj in objs)
			{
				// Column Names Row (First Row)
				if (columnNames.Count == 0)
				{
					bool isFirst = true;
					foreach (var column in obj)
					{
						if (!isFirst)
							writer.Write (separator);
						columnNames.Add (column.Key);
						writer.Write (SafeText (column.Key, separator), true);
						isFirst = false;
					}
					writer.WriteLine ();
				}

				// Columns Row
				{
					bool isFirst = true;
					for (int i = 0; i < obj.Count; ++i)
					{
						if (!isFirst)
							writer.Write (separator);
						var currentColumnName = columnNames [i];
						writer.Write (SafeText (obj.Get (currentColumnName).ToString (), separator));
					}
					writer.WriteLine ();
				}
			}
		}
#endregion

#region Deserialization
		public static IEnumerable<BlockarObject> DeserializeFromCsv (Stream stream, char separator = CsvSeparatorDetectorCharacter)
		{
#if NET20
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true);
#else
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true);
#endif
			return DeserializeFromCsv (reader, separator);
		}

		public static IEnumerable<BlockarObject> DeserializeFromCsv (string csv, char separator = CsvSeparatorDetectorCharacter)
		{
			TextReader reader = new StringReader (csv);
			return DeserializeFromCsv (reader, separator);
		}

		public static IEnumerable<BlockarObject> DeserializeFromCsv (TextReader reader, char separator = CsvSeparatorDetectorCharacter)
		{
			string columnNameRow = reader.ReadLine ();
			if (separator == CsvSeparatorDetectorCharacter)
			{
#if NET20
				if (columnNameRow.IndexOf (',') >= 0) separator = ',';
				else if (columnNameRow.IndexOf ('\t') >= 0) separator = '\t';
				else if (columnNameRow.IndexOf ('|') >= 0) separator = '|';
#else
				if (columnNameRow.Contains (',')) separator = ',';
				else if (columnNameRow.Contains ('\t')) separator = '\t';
				else if (columnNameRow.Contains ('|')) separator = '|';
#endif
				else throw new ArgumentException ("Unknown Separator.");
			}

			List<string> columnNames = new List<string> (columnNameRow.Split (separator));

			BlockarObject obj = null;
			CsvDeserializeState state = CsvDeserializeState.StartRow;
			StringBuilder builder = new StringBuilder ();
			int columnNumber = 0;
			while (true)
			{
				char ch = (char) reader.Peek ();
				if (ch == 0xffff)
					break;

				switch (state)
				{
					case CsvDeserializeState.StartRow:
						{
							obj = new BlockarObject ();
							state = CsvDeserializeState.StartColumn;
							columnNumber = 0;
						}
						break;

					case CsvDeserializeState.StartColumn:
						{
							ch = (char) reader.Read ();
							if (ch == '"')
							{
								state = CsvDeserializeState.WrappedColumning;
							}
							else if (ch == '\r')
								continue;
							else if (ch == '\n')
							{
								if (obj.Count != 0)
								{
									yield return obj;
									obj = null;
								}
								state = CsvDeserializeState.StartRow;
							}
							else
							{
								state = CsvDeserializeState.Columning;
								builder.Append (ch);
							}
						}
						break;

					case CsvDeserializeState.Columning:
						{
							if (ch == '\n')
							{
								state = CsvDeserializeState.EndColumn;
							}
							else
							{
								ch = (char) reader.Read ();
								if (ch == separator)
								{
									state = CsvDeserializeState.EndColumn;
								}
								else
									builder.Append (ch);
							}
						}
						break;

					case CsvDeserializeState.WrappedColumning:
						{
							if (builder.Length > 0 && builder [builder.Length - 1] == '"' && ch == '\n')
							{
								builder.Remove (builder.Length - 1, 1);
								state = CsvDeserializeState.EndColumn;
							}
							else
							{
								ch = (char) reader.Read ();
								builder.Append (ch);
								if (builder.Length >= 2)
								{
									if (builder [builder.Length - 2] == '"' && builder [builder.Length - 1] == separator)
									{
										builder.Remove (builder.Length - 2, 2);
										state = CsvDeserializeState.EndColumn;
									}
								}
							}
						}
						break;

					case CsvDeserializeState.EndColumn:
						{
							obj.Set (columnNames [columnNumber], builder.ToString ());
#if NET20
							builder = new StringBuilder ();
#else
							builder.Clear ();
#endif
							++columnNumber;
							state = CsvDeserializeState.StartColumn;
						}
						break;
				}
			}

			if (obj != null && obj.Count > 0)
				yield return obj;
		}

		private enum CsvDeserializeState
		{
			StartRow,
			StartColumn,
			Columning,
			WrappedColumning,
			EndColumn,
		}
#endregion
	}
}
