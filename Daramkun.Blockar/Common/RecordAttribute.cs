using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Common
{
	[AttributeUsage ( AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
	public class RecordAttribute : Attribute { public string Name { get; set; } }
}
