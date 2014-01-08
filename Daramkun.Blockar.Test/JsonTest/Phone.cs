using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Json;

namespace Daramkun.Blockar.Test.JsonTest
{
	public class Phone : IJsonContainer
	{
		string type;
		string platform;
		string name;
		string number;

		public ContainType ContainerType { get { return ContainType.Object; } }

		[Record ( "type" )]
		public string Type { get { return type; } set { type = value; } }
		[Record ( "platform" )]
		public string Platform { get { return platform; } set { platform = value; } }
		[Record ( "name" )]
		public string Name { get { return name; } set { name = value; } }
		[Record ( "number" )]
		public string Number { get { return number; } set { number = value; } }

		public JsonContainer ToJsonContainer ()
		{
			return JsonProvider.ToJsonContainer ( this );
		}

		public IJsonContainer FromJsonContainer ( JsonContainer entry )
		{
			Type = entry [ "type" ] as string;
			Platform = entry [ "platform" ] as string;
			Name = entry [ "name" ] as string;
			Number = entry [ "number" ] as string;
			return this;
		}

		public override string ToString ()
		{
			return ToJsonContainer ().ToString ();
		}
	}
}
