using System;
using System.Collections.Generic;
using System.Text;

namespace Daramee.Blockar
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public class SectionNameAttribute : Attribute
	{
		public string Name { get; }

		public SectionNameAttribute (string name)
		{
			Name = name;
		}
	}
}
