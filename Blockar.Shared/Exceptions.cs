using System;
using System.Collections.Generic;
using System.Text;

namespace Daramee.Blockar
{
	public class BlockarJsonDeserializeException : Exception
	{
		public BlockarJsonDeserializeException ()
			: base ("Invalid JSON Document.")
		{

		}
		public BlockarJsonDeserializeException (Exception innerException)
			: base ("Invalid JSON Document.", innerException)
		{

		}
	}
}
