using Daramee.Blockar;
using System;

namespace Blockar.Test
{
	class Program
	{
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

			Person person = new Person () { Name = "Jane Doe", Age = 29 };
			Console.WriteLine (BlockarObject.FromObject (person).ToJsonString ());

			string jsonString = "{\"name\": \"Mei\", \"age\": 30}";
			blockarObject = new BlockarObject ();
			blockarObject.DeserializeFromJson (jsonString);
			Console.WriteLine (blockarObject.ToJsonString ());
		}
	}
}
