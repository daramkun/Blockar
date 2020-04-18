using Daramee.Blockar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Blockar.Test
{
	class Program
	{
		[SectionName ("Person")]
		class Person
		{
			[FieldOption (Name = "name")]
			public string Name;
			[FieldOption (Name = "age")]
			public int Age;
		}

		static void Main (string [] args)
		{
			BlockarObject blockarObject = new BlockarObject ();
			blockarObject.Set<string> ("name", "John Doe");
			blockarObject.Set<int> ("age", 31);
			Console.WriteLine (blockarObject.ToJsonString ());
			Console.WriteLine (blockarObject.ToIniString ());

			Person person = new Person () { Name = "Jane Doe", Age = 29 };
			Console.WriteLine (BlockarObject.FromObject (person).ToJsonString ());
			Console.WriteLine (BlockarObject.FromObject (person).ToIniString ());

			string jsonString = "{\"name\": \"Mei\", \"age\": 30}";
			blockarObject = BlockarObject.DeserializeFromJson (jsonString);
			Console.WriteLine (blockarObject.ToJsonString ());

			using (Stream r6sSettingsStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.R6SGameSettings.ini"))
			{
				List<BlockarObject> r6sSettings = new List<BlockarObject> (BlockarObject.DeserializeFromIni (r6sSettingsStream));
			}

			using (Stream lottoStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.KoreaLotto.csv"))
			{
				List<BlockarObject> lottoNumbers = new List<BlockarObject> (BlockarObject.DeserializeFromCsv (lottoStream));
			}

		}
	}
}
