using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Common;
using Daramkun.Blockar.Json;

namespace Daramkun.Blockar.Test.JsonTest
{
	public class Phone
	{
		string type;
		string platform;
		string name;
		string number;

		public ContainType ContainerType { get { return ContainType.Object; } }

		[Record ( Name = "type" )]
		public string Type { get { return type; } set { type = value; } }
		[Record ( Name = "platform" )]
		public string Platform { get { return platform; } set { platform = value; } }
		[Record ( Name = "name" )]
		public string Name { get { return name; } set { name = value; } }
		[Record ( Name = "number" )]
		public string Number { get { return number; } set { number = value; } }

		public override string ToString ()
		{
			return new JsonContainer ( this ).ToString ();
		}
	}
}
