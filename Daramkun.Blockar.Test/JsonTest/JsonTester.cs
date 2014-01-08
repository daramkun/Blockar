using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Json;

namespace Daramkun.Blockar.Test.JsonTest
{
	public static class JsonTester
	{
		public static void Run ()
		{
			JsonContainer certification = new JsonContainer ( ContainType.Array );
			certification.Add ( "Word Processor" );
			certification.Add ( "Expert of Game Programming" );
			certification.Add ( "ITQ PowerPoint" );

			JsonContainer phone = new JsonContainer ( ContainType.Object );
			phone.Add ( "Smartphone", "type" );
			phone.Add ( "Windows Phone", "platform" );
			phone.Add ( "HTC 7 Mozart", "name" );
			phone.Add ( "010-xxxx-xxxx", "number" );

			JsonContainer json = new JsonContainer ( ContainType.Object );
			json.Add ( "Jin Jae-yeon", "name" );
			json.Add ( 24, "age" );
			json.Add ( 168.7, "height" );
			json.Add ( phone, "phone" );
			json.Add ( certification, "certification" );

			Console.WriteLine ( "=========== Object Json ===========" );
			Console.WriteLine ( json );

			string jsonString = json.ToString ();

			Console.WriteLine ( "=========== Parsed from String ===========" );
			Console.WriteLine ( JsonParser.Parse ( Assembly.GetExecutingAssembly ().GetManifestResourceStream ( "Daramkun.Blockar.Test.json1.json" ) ) );

			Console.WriteLine ( "=========== Parsed from Object ===========" );
			Console.WriteLine ( JsonParser.Parse ( json.ToString () ) );

			People myPeople = new People ()
			{
				Name = "Jin Jae-yeon",
				Age = 22,
				Height = 168.7f,
				Phone = new Phone () { Type = "Smartphone", Platform = "Windows Phone", Name = "HTC 7 Mozart", Number = "010-xxxx-xxxx" },
				Certifications = new string [] { "Word Processor", "Expert of Game Programming", "ITQ PowerPoint" }
			}, myPeople2 = new People ();

			Console.WriteLine ( "=========== Custom Json Object ===========" );
			Console.WriteLine ( myPeople );

			Console.WriteLine ( "=========== Custom Json Object to Custom Json Object by Json String ===========" );
			Console.WriteLine ( myPeople2.FromJsonContainer ( myPeople.ToJsonContainer () ).ToString () );

			Console.WriteLine ( "=========== Benchmark ===========" );

			int loopCount = 100000;

			int start = Environment.TickCount;
			for ( int i = 0; i < loopCount; i++ )
				JsonParser.Parse ( jsonString );
			int end = Environment.TickCount;
			Console.WriteLine ( String.Format ( "위 Json 데이터 {0:0,0}번 파싱하는데 걸린 시간 : {1:0.000}sec", loopCount, ( end - start ) / 1000.0f ) );
		}
	}
}
