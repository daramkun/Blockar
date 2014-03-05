using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Daramkun.Blockar.Common;
using Daramkun.Blockar.Json;

namespace Daramkun.Blockar.Test.JsonTest
{
	public class People : IJsonContainer
	{
		string name;
		int age;
		float height;
		Phone phone;
		string [] certifications;
		string description;

		public ContainType ContainerType { get { return ContainType.Object; } }

		[Record ( "name" )]
		public string Name { get { return name; } set { name = value; } }
		[Record ( "age" )]
		public int Age { get { return age; } set { age = value; } }
		[Record ( "height" )]
		public float Height { get { return height; } set { height = value; } }
		[Record ( "phone" )]
		public Phone Phone { get { return phone; } set { phone = value; } }
		[Record ( "certification" )]
		public string [] Certifications { get { return certifications; } set { certifications = value; } }
		[Record ( "description" )]
		public string Description { get { return description; } set { description = value; } }

		public People () { Description = ""; }

		public JsonContainer ToJsonContainer ()
		{
			return JsonProvider.ToJsonContainer ( this );
		}

		public IJsonContainer FromJsonContainer ( JsonContainer entry )
		{
			if ( entry == null ) throw new ArgumentNullException ();
			Name = entry [ "name" ] as string;
			Age = ( int ) entry [ "age" ];
			Height = ( float ) entry [ "height" ];
			Phone = new Phone ();
			Phone.FromJsonContainer ( entry [ "phone" ] as JsonContainer );
			List<string> cert = new List<string> ();
			foreach ( object item in ( entry [ "certification" ] as JsonContainer ).GetListEnumerable () )
				cert.Add ( item as string );
			certifications = cert.ToArray ();
			description = entry [ "description" ] as string;
			return this;
		}

		public override string ToString ()
		{
			return ToJsonContainer ().ToString ();
		}
	}
}
