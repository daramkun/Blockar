using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NET46
using System.Numerics;
#endif
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	public delegate bool ValueTypeCheck (Type type);
	public delegate object ValueConvert (object value, Type type);

	public static class ValueConverter
	{
		private static readonly IDictionary<ValueTypeCheck, ValueConvert> ValueConverters = new Dictionary<ValueTypeCheck, ValueConvert> ()
		{
			{ (type) => type == typeof (byte), (value, type) => Convert.ToByte (value) },
			{ (type) => type == typeof (sbyte), (value, type) => Convert.ToSByte (value) },
			{ (type) => type == typeof (short), (value, type) => Convert.ToInt16 (value) },
			{ (type) => type == typeof (ushort), (value, type) => Convert.ToUInt16 (value) },
			{ (type) => type == typeof (int), (value, type) => Convert.ToInt32 (value) },
			{ (type) => type == typeof (uint), (value, type) => Convert.ToUInt32 (value) },
			{ (type) => type == typeof (long), (value, type) => Convert.ToInt64 (value) },
			{ (type) => type == typeof (ulong), (value, type) => Convert.ToUInt64 (value) },
			{ (type) => type == typeof (IntPtr), (value, type) =>
				new IntPtr (
#if !NET20 && !NET35
					Environment.Is64BitProcess ? Convert.ToInt64 (value) :
#endif
					Convert.ToInt32 (value)
				)
			},
			{ (type) => type == typeof (float), (value, type) => Convert.ToSingle (value) },
			{ (type) => type == typeof (double), (value, type) => Convert.ToDouble (value) },
			{ (type) => type == typeof (decimal), (value, type) => Convert.ToDecimal (value) },
			{ (type) => type == typeof (bool), (value, type) => Convert.ToBoolean (value) },
			{ (type) => type == typeof (char), (value, type) => Convert.ToChar (value) },
			{ (type) => type == typeof (string), (value, type) => value is string ? value : value.ToString ()},
			{ (type) => type == typeof (Regex), (value, type) => new Regex (value.ToString () ?? string.Empty) },
			{ (type) => type == typeof (DateTime), (value, type) => Convert.ToDateTime (value) },
			{ (type) => type == typeof (TimeSpan), (value, type) =>
			{
				switch (value)
				{
					case byte _:
					case sbyte _:
					case short _:
					case ushort _:
					case int _:
					case uint _:
					case long _:
					case ulong _:
						return TimeSpan.FromTicks ((long) value);
					case float _:
					case double _:
					case decimal _:
						return TimeSpan.FromSeconds ((double) value);
					default:
						return TimeSpan.TryParse(value?.ToString(), out var result) ? (object) result : null;
				}
			} },
			{ (type) => type == typeof (BlockarObject), (value, type) => BlockarObject.FromObject(value.GetType(), value)},
			{ (type) => type.GetInterface ("IDictionary") != null, (value, type) => {
				var newDict = Activator.CreateInstance (type) as IDictionary;
				var valueType = value.GetType ();

				if (!(value is IDictionary valueDict))
					valueDict = BlockarObject.FromObject(value.GetType (), value).ToDictionary ();

				var genericTypes = type.GetGenericArguments ();
				var dictKeyType = genericTypes?.GetValue (0) as Type ?? typeof (object);
				var dictValueType = genericTypes?.GetValue (1) as Type ?? typeof (object);
				foreach(var key in valueDict.Keys)
				{
					var newKey = dictKeyType != typeof (object)
						? ValueConvert (key, dictKeyType)
						: key;
					var dictCurrentValue = valueDict [key];
					var newValue = dictValueType != typeof (object)
						? ValueConvert (dictCurrentValue, dictValueType)
						: dictCurrentValue;
					newDict.Add (newKey, newValue);
				}

				return newDict;
			} },
			{ (type) => type.GetInterface("IList") != null, (value, type) => {
				var newList = Activator.CreateInstance (type) as IList;
				var valueType = value.GetType ();

				var valueEnum = value as IEnumerator;
				if(value is string str)
					valueEnum = Encoding.UTF8.GetBytes (str).GetEnumerator ();
				valueEnum ??= value is IEnumerable enumerable ? enumerable.GetEnumerator() : new[] {value}.GetEnumerator();

				var genericTypes = type.GetGenericArguments ();
				var genericType = genericTypes?.GetValue (0) as Type ?? typeof (object);

				while (valueEnum.MoveNext ())
					newList.Add (ValueConverter.ValueConvert (valueEnum.Current, genericType));

				return newList;
			} },
			{ type => type.IsArray, (value, type) => {
				if(value is string str)
					value = Encoding.UTF8.GetBytes (str);
				if(value is IEnumerable enumerable)
				{
					var elementType = type.GetElementType ();
					var temp = (from object i in enumerable select ValueConverter.ValueConvert(i, elementType)).ToList();
					var arr = Array.CreateInstance (elementType, temp.Count);
					Array.Copy (temp.ToArray (), arr, temp.Count);
					return arr;
				}
				else if (value.GetType ().IsArray)
				{
					var elementType = type.GetElementType();
					var valueArr = value as Array;
					var arr = Array.CreateInstance(elementType, valueArr.Length);
					for (var i = 0; i < arr.Length; ++i)
						arr.SetValue (ValueConvert (valueArr.GetValue (i), elementType), i);
				}
				return null;
			} },
			{ type => type.IsSubclassOf (typeof (Enum)) || type == typeof (Enum), (value, type) => {
				try
				{
					return Enum.Parse(type, value.ToString()!, false);
				}
				catch
				{
					return ValueConvert((int) value, type);
				}
			} },
#if NET46
			{ (type) => type == typeof (Vector2), (value, type) => {
				if (value.GetType ().IsArray)
				{
					var arr = value as Array;
					return new Vector2 (
						(float) ValueConverter.ValueConvert (arr.GetValue (0), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (1), typeof (float))
					);
				}
				else if (value is IDictionary)
				{
					float x = 0, y = 0;
					if((value as IDictionary).Contains("x"))
						x = (float) ValueConverter.ValueConvert ((value as IDictionary) ["x"], typeof (float));
					if((value as IDictionary).Contains("y"))
						y = (float) ValueConverter.ValueConvert ((value as IDictionary) ["y"], typeof (float));
					return new Vector2 (x, y);
				}
				return BlockarObject.FromObject (value.GetType (), value).ToObject (typeof (Vector2));
			} },
			{ (type) => type == typeof (Vector3), (value, type) => {
				if (value.GetType ().IsArray)
				{
					var arr = value as Array;
					return new Vector3 (
						(float) ValueConverter.ValueConvert (arr.GetValue (0), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (1), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (2), typeof (float))
					);
				}
				else if (value is IDictionary)
				{
					float x = 0, y = 0, z = 0;
					if((value as IDictionary).Contains("x"))
						x = (float) ValueConverter.ValueConvert ((value as IDictionary) ["x"], typeof (float));
					if((value as IDictionary).Contains("y"))
						y = (float) ValueConverter.ValueConvert ((value as IDictionary) ["y"], typeof (float));
					if((value as IDictionary).Contains("z"))
						z = (float) ValueConverter.ValueConvert ((value as IDictionary) ["z"], typeof (float));
					return new Vector3 (x, y, z);
				}
				return BlockarObject.FromObject (value.GetType (), value).ToObject (typeof (Vector3));
			} },
			{ (type) => type == typeof (Vector4), (value, type) => {
				if (value.GetType ().IsArray)
				{
					var arr = value as Array;
					return new Vector4 (
						(float) ValueConverter.ValueConvert (arr.GetValue (0), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (1), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (2), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (3), typeof (float))
					);
				}
				else if (value is IDictionary)
				{
					float x = 0, y = 0, z = 0, w = 0;
					if((value as IDictionary).Contains("x"))
						x = (float) ValueConverter.ValueConvert ((value as IDictionary) ["x"], typeof (float));
					if((value as IDictionary).Contains("y"))
						y = (float) ValueConverter.ValueConvert ((value as IDictionary) ["y"], typeof (float));
					if((value as IDictionary).Contains("z"))
						z = (float) ValueConverter.ValueConvert ((value as IDictionary) ["z"], typeof (float));
					if((value as IDictionary).Contains("w"))
						w = (float) ValueConverter.ValueConvert ((value as IDictionary) ["w"], typeof (float));
					return new Vector4 (x, y, z, w);
				}
				return BlockarObject.FromObject (value.GetType (), value).ToObject (typeof (Vector4));
			} },
			{ (type) => type == typeof (Quaternion), (value, type) => {
				if (value.GetType ().IsArray)
				{
					var arr = value as Array;
					return new Quaternion (
						(float) ValueConverter.ValueConvert (arr.GetValue (0), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (1), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (2), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (3), typeof (float))
					);
				}
				else if (value is IDictionary)
				{
					float x = 0, y = 0, z = 0, w = 0;
					if((value as IDictionary).Contains("x"))
						x = (float) ValueConverter.ValueConvert ((value as IDictionary) ["x"], typeof (float));
					if((value as IDictionary).Contains("y"))
						y = (float) ValueConverter.ValueConvert ((value as IDictionary) ["y"], typeof (float));
					if((value as IDictionary).Contains("z"))
						z = (float) ValueConverter.ValueConvert ((value as IDictionary) ["z"], typeof (float));
					if((value as IDictionary).Contains("w"))
						w = (float) ValueConverter.ValueConvert ((value as IDictionary) ["w"], typeof (float));
					return new Quaternion (x, y, z, w);
				}
				return BlockarObject.FromObject (value.GetType (), value).ToObject (typeof (Quaternion));
			} },
			{ (type) => type == typeof (Matrix4x4), (value, type) => {
				if (value.GetType ().IsArray)
				{
					var arr = value as Array;
					return new Matrix4x4 (
						(float) ValueConverter.ValueConvert (arr.GetValue (0), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (1), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (2), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (3), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (4), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (5), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (6), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (7), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (8), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (9), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (10), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (11), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (12), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (13), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (14), typeof (float)),
						(float) ValueConverter.ValueConvert (arr.GetValue (15), typeof (float))
					);
				}
				return BlockarObject.FromObject (value.GetType (), value).ToObject (typeof (Matrix4x4));
			} },
#endif
		};

		public static object ValueConvert (object value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException ();
			if (value == null || value.GetType () == type)
				return value;

			foreach (var kv in ValueConverters)
			{
				if (kv.Key (type))
					return kv.Value (value, type);
			}

			return BlockarObject.FromObject (value.GetType (), value).ToObject (type);
		}

		public static void RegisterValueConverter (ValueTypeCheck checker, ValueConvert converter)
		{
			if (ValueConverters.ContainsKey (checker))
				return;
			ValueConverters.Add (checker, converter);
		}
	}
}
