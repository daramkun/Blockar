using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daramkun.Blockar.Json
{
	internal enum BSONType
	{
		EndDoc = 0,
		Double = 0x01,
		String = 0x02,
		Document = 0x03,
		Array = 0x04,
		BinaryData = 0x05,
		Boolean = 0x08,
		UTCTime = 0x09,
		Null = 0x0A,
		Regexp = 0x0B,
		JavascriptCode = 0x0D,
		JavascriptCodeWScope = 0x0F,
		Integer = 0x10,
		Integer64 = 0x12,
	}
}
