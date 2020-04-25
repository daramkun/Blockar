using Daramee.Blockar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		static void BaseTest ()
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

			using (Stream daramRenamerStringTableStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.DaramRenamerStringTable.json"))
			{
				BlockarObject daramRenamerStringTable = BlockarObject.DeserializeFromJson (daramRenamerStringTableStream);
			}

			using (Stream r6sSettingsStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.R6SGameSettings.ini"))
			{
				List<BlockarObject> r6sSettings = new List<BlockarObject> (BlockarObject.DeserializeFromIni (r6sSettingsStream));
			}

			using (Stream lottoStream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.KoreaLotto.csv"))
			{
				List<BlockarObject> lottoNumbers = new List<BlockarObject> (BlockarObject.DeserializeFromCsv (lottoStream));
			}
		}

		static void JsonPerformanceTest ()
		{
			const int LOOP_COUNT = 100;
			string jsonString = new StreamReader (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("Blockar.Test.Samples.DaramRenamerStringTable.json")).ReadToEnd ();
			Stopwatch stopwatch = new Stopwatch ();

			Console.WriteLine ("== Blockar JSON Deserializer Performance Test ==");
			
			stopwatch.Start ();
			for (int i = 0; i < LOOP_COUNT; ++i)
				BlockarObject.DeserializeFromJson (jsonString);
			Console.WriteLine ("Blockar: {0}s per {1}", stopwatch.Elapsed.TotalSeconds, LOOP_COUNT);
			stopwatch.Stop ();

			stopwatch.Restart ();
			var jsonDotNetSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault ();
			for (int i = 0; i < LOOP_COUNT; ++i)
			{
				using (TextReader reader = new StringReader (jsonString))
					jsonDotNetSerializer.Deserialize (reader, null);
			}
			Console.WriteLine ("Newtonsoft.Json: {0}s per {1}", stopwatch.Elapsed.TotalSeconds, LOOP_COUNT);
			stopwatch.Stop ();

		}

		static void Main (string [] args)
		{
			//BaseTest ();
			JsonPerformanceTest ();
		}
	}
}
