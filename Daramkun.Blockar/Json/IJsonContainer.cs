using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Json
{
	public interface IJsonContainer
	{
		ContainType ContainerType { get; }
		JsonContainer ToJsonContainer ();
		IJsonContainer FromJsonContainer ( JsonContainer container );
	}
}
