using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Json
{
	[AttributeUsage ( AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
	public class InnerArrayAttribute : Attribute
	{
		
	}
}
