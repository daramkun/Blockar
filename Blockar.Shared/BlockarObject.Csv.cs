using System;
using System.Collections.Generic;
using System.IO;
#if !NET20 && !NET35
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
#if NET20 || NET35
			using (var writer = new StreamWriter (stream, Encoding.UTF8))
#else
			using (var writer = new StreamWriter (stream, Encoding.UTF8, 4096, true))
#endif
			{
				SerializeToCsv (writer, separator, objs);
			}
		}

		public static string SafeText (string text, char separator, bool isForColumnName = false)
		{
			if (isForColumnName
#if NET20 || NET35
				&& (text.IndexOf (separator) >= 0 || text.IndexOf (',') >= 0 || text.IndexOf ('|') >= 0 || text.IndexOf ('\t') >= 0 || text.IndexOf ('"') >= 0 || text.IndexOf ('\n') >= 0))
#else
				&& (text.Contains (separator) || text.Contains (',') || text.Contains ('|') || text.Contains ('\t') || text.Contains ('"') || text.Contains ('\n')))
#endif
				throw new ArgumentException ($"Column Name cannot contains ',', '|', '\\t', '\"', '\\n' and ${separator}.");

#if NET20 || NET35
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
			var columnNames = new List<string> ();
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
					var isFirst = true;
					for (var i = 0; i < obj.Count; ++i)
					{
						if (!isFirst)
							writer.Write (separator);
						isFirst = false;
						var currentColumnName = columnNames [i];
						writer.Write (SafeText (obj.Get (currentColumnName).ToString (), separator));
					}
					writer.WriteLine ();
				}
			}
		}
#endregion

#region Deserialization
		public static IEnumerable<BlockarObject> DeserializeFromCsv (Stream stream, bool requireTitleRow = true, char separator = CsvSeparatorDetectorCharacter)
		{
#if NET20 || NET35
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true);
#else
			TextReader reader = new StreamReader (stream, Encoding.UTF8, true, 4096, true);
#endif
			return DeserializeFromCsv (reader, requireTitleRow, separator);
		}

		public static IEnumerable<BlockarObject> DeserializeFromCsv (string csv, bool requireTitleRow = true, char separator = CsvSeparatorDetectorCharacter)
		{
			TextReader reader = new StringReader (csv);
			return DeserializeFromCsv (reader, requireTitleRow, separator);
		}

		public static IEnumerable<BlockarObject> DeserializeFromCsv (TextReader reader, bool requireTitleRow = true, char separator = CsvSeparatorDetectorCharacter)
		{
			List<string> columnNames;
			if (separator == CsvSeparatorDetectorCharacter)
			{
				var columnNameRow = reader.ReadLine ();
				if (requireTitleRow)
				{
#if NET20 || NET35
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
				else
					throw new ArgumentException ("Cannot Separator Detection Non-required Title row.");

				columnNames = new List<string> (columnNameRow.Split (separator));
			}
			else
				columnNames = new List<string> ();

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
							switch (ch)
							{
								case '"':
									state = CsvDeserializeState.WrappedColumning;
									break;
								case '\r':
									continue;
								case '\n':
									{
										if (obj.Count != 0)
										{
											yield return obj;
											obj = null;
										}
										state = CsvDeserializeState.StartRow;
										break;
									}
								default:
									state = CsvDeserializeState.Columning;
									builder.Append (ch);
									break;
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
							if (!requireTitleRow && columnNames.Count <= columnNumber)
								columnNames.Add ((columnNumber + 1).ToString ());
							obj.Set (columnNames [columnNumber], builder.ToString ());
#if NET20 || NET35
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
