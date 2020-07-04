using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
#if NET20
	[AttributeUsage (AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
#else
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
#endif
	public class FieldOptionAttribute : Attribute
	{
		string _name;

		public string Name
		{
			get => _name;
			set
			{
				if (!Regex.IsMatch (value, "[a-zA-Z0-9가-힣\\-_ %@#!&^*+/~`]*"))
					throw new ArgumentException ();
				_name = value;
			}
		}
		public bool IsRequired { get; set; }
	}
}
