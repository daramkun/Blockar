using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Json
{
	partial class JsonContainer
	{
		public void Serialize(object obj)
		{
			
		}

		public T Deserialize<T> ()
		{
			T obj = Activator.CreateInstance<T> ();



			return obj;
		}
	}
}
