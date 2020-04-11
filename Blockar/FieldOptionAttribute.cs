using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class FieldOptionAttribute : Attribute
	{
		string name;

		public string Name
		{
			get => name;
			set
			{
				if (!Regex.IsMatch (value, "[a-zA-Z0-9가-힣\\-_ %@#!&^*+/~`]*"))
					throw new ArgumentException ();
				name = value;
			}
		}
		public bool IsRequired { get; set; }
	}
}
