using System;
using System.Collections;
using System.Collections.Generic;
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
		static readonly IDictionary<ValueTypeCheck, ValueConvert> valueConverters = new Dictionary<ValueTypeCheck, ValueConvert> ()
		{
			{ (type) => type == typeof (byte), (value, type) => System.Convert.ToByte (value) },
			{ (type) => type == typeof (sbyte), (value, type) => System.Convert.ToSByte (value) },
			{ (type) => type == typeof (short), (value, type) => System.Convert.ToInt16 (value) },
			{ (type) => type == typeof (ushort), (value, type) => System.Convert.ToUInt16 (value) },
			{ (type) => type == typeof (int), (value, type) => System.Convert.ToInt32 (value) },
			{ (type) => type == typeof (uint), (value, type) => System.Convert.ToUInt32 (value) },
			{ (type) => type == typeof (long), (value, type) => System.Convert.ToInt64 (value) },
			{ (type) => type == typeof (ulong), (value, type) => System.Convert.ToUInt64 (value) },
			{ (type) => type == typeof (IntPtr), (value, type) =>
				new IntPtr (
#if !NET20 && !NET35
					Environment.Is64BitProcess ? System.Convert.ToInt64 (value) :
#endif
					System.Convert.ToInt32 (value)
				)
			},
			{ (type) => type == typeof (float), (value, type) => System.Convert.ToSingle (value) },
			{ (type) => type == typeof (double), (value, type) => System.Convert.ToDouble (value) },
			{ (type) => type == typeof (decimal), (value, type) => System.Convert.ToDecimal (value) },
			{ (type) => type == typeof (bool), (value, type) => System.Convert.ToBoolean (value) },
			{ (type) => type == typeof (char), (value, type) => System.Convert.ToChar (value) },
			{ (type) => type == typeof (string), (value, type) => {
				if (value is string) return value;
				else return value.ToString ();
			} },
			{ (type) => type == typeof (Regex), (value, type) => new Regex (value.ToString ()) },
			{ (type) => type == typeof (DateTime), (value, type) => System.Convert.ToDateTime (value) },
			{ (type) => type == typeof (TimeSpan), (value, type) => {
				if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong)
					return TimeSpan.FromTicks ((long) value);
				else if (value is float || value is double || value is decimal)
					return TimeSpan.FromSeconds ((double) value);
				else if(TimeSpan.TryParse(value?.ToString (), out TimeSpan result))
					return result;
				else
					return null;
			} },
			{ (type) => type == typeof (BlockarObject), (value, type) => {
				return BlockarObject.FromObject(value.GetType(), value);
			} },
			{ (type) => type.GetInterface ("IDictionary") != null, (value, type) => {
				IDictionary newDict = Activator.CreateInstance (type) as IDictionary;
				var valueType = value.GetType ();

				IDictionary valueDict = value as IDictionary;
				if (valueDict == null)
					valueDict = BlockarObject.FromObject(value.GetType (), value).ToDictionary ();

				var genericTypes = type.GetGenericArguments ();
				Type dictKeyType = genericTypes?.GetValue (0) as Type ?? typeof (object);
				Type dictValueType = genericTypes?.GetValue (1) as Type ?? typeof (object);
				foreach(var key in valueDict.Keys)
				{
					var newKey = dictKeyType != typeof (object)
						? ValueConverter.ValueConvert (key, dictKeyType)
						: key;
					var dictCurrentValue = valueDict [key];
					var newValue = dictValueType != typeof (object)
						? ValueConverter.ValueConvert (dictCurrentValue, dictValueType)
						: dictCurrentValue;
					newDict.Add (newKey, newValue);
				}

				return newDict;
			} },
			{ (type) => type.GetInterface("IList") != null, (value, type) => {
				IList newList = Activator.CreateInstance (type) as IList;
				var valueType = value.GetType ();

				IEnumerator valueEnum = value as IEnumerator;
				if(value is string)
					valueEnum = Encoding.UTF8.GetBytes (value as string).GetEnumerator ();
				if(valueEnum == null)
				{
					if (value is IEnumerable)
						valueEnum = (value as IEnumerable).GetEnumerator ();
					else
						valueEnum = new object [] { value }.GetEnumerator ();
				}

				var genericTypes = type.GetGenericArguments ();
				Type genericType = genericTypes?.GetValue (0) as Type ?? typeof (object);

				while (valueEnum.MoveNext ())
					newList.Add (ValueConverter.ValueConvert (valueEnum.Current, genericType));

				return newList;
			} },
			{ (type) => type.IsArray, (value, type) => {
				if(value is string)
					value = Encoding.UTF8.GetBytes (value as string);
				if(value is IEnumerable)
				{
					List<object> temp = new List<object> ();
					var elementType = type.GetElementType ();
					foreach(object i in value as IEnumerable)
						temp.Add (ValueConverter.ValueConvert (i, elementType));
					Array arr = Array.CreateInstance (elementType, temp.Count);
					Array.Copy (temp.ToArray (), arr, temp.Count);
					return arr;
				}
				else if (value.GetType ().IsArray)
				{
					Type elementType = type.GetElementType();
					Array valueArr = value as Array;
					Array arr = Array.CreateInstance(elementType, valueArr.Length);
					for (int i = 0; i < arr.Length; ++i)
						arr.SetValue (ValueConverter.ValueConvert (valueArr.GetValue (i), elementType), i);
				}
				return null;
			} },
			{ (type) => type.IsSubclassOf (typeof (Enum)) || type == typeof (Enum), (value, type) => {
				try
				{
					return Enum.Parse (type, value.ToString (), false);
				}
				catch { return null; }
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
			else
			{
				foreach (var kv in valueConverters)
				{
					if (kv.Key (type))
						return kv.Value (value, type);
				}

				return BlockarObject.FromObject (value.GetType (), value).ToObject (type);
			}
		}

		public static void RegisterValueConverter (ValueTypeCheck checker, ValueConvert converter)
		{
			if (valueConverters.ContainsKey (checker))
				return;
			valueConverters.Add (checker, converter);
		}
	}
}
