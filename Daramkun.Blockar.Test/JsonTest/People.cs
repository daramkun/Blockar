using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Common;
using Daramkun.Blockar.Json;

namespace Daramkun.Blockar.Test.JsonTest
{
	public class People
	{
		string name;
		int age;
		float height;
		Phone phone;
		string [] certifications;
		string description;

		public ContainType ContainerType { get { return ContainType.Object; } }

		[Record ( Name = "name" )]
		public string Name { get { return name; } set { name = value; } }
		[Record ( Name = "age" )]
		public int Age { get { return age; } set { age = value; } }
		[Record ( Name = "height" )]
		public float Height { get { return height; } set { height = value; } }
		[Record ( Name = "phone" )]
		public Phone Phone { get { return phone; } set { phone = value; } }
		[Record ( Name = "certification" )]
		public string [] Certifications { get { return certifications; } set { certifications = value; } }
		[Record ( Name = "description" )]
		public string Description { get { return description; } set { description = value; } }

		public People () { Description = ""; }

		public override string ToString ()
		{
			return new JsonContainer ( this ).ToString ();
		}
	}
}
