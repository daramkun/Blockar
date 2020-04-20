using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	public class ObjectKeyValue
	{
		public readonly string Key;
		public object Value;

		public ObjectKeyValue (string key) { Key = key; Value = default; }
		public ObjectKeyValue (string key, object value = null) { Key = key; Value = value; }
		public ObjectKeyValue (ObjectKeyValue kv) { Key = kv.Key; Value = kv.Value; }
		public override string ToString () => $"{{{Key}:{Value}}}";
	}

	public class ObjectKeyValue<T> : ObjectKeyValue
	{
		public new T Value;

		public ObjectKeyValue (string key, T value) : base (key) { Value = value; }
	}

	public delegate bool ValueTypeCheck (Type type);
	public delegate object ValueConverter (object value, Type type);

	public sealed partial class BlockarObject : IEnumerable<ObjectKeyValue>, IEnumerable<string>, IEnumerable<object>
	{
		public static IDictionary<ValueTypeCheck, ValueConverter> ValueConverters { get; private set; } = new Dictionary<ValueTypeCheck, ValueConverter> ()
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
#if !NET20
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
			{ (type) => type == typeof (string), (value, type) => {
				if (value is string) return value;
				else return value.ToString ();
			} },
			{ (type) => type == typeof (Regex), (value, type) => new Regex (value.ToString ()) },
			{ (type) => type == typeof (DateTime), (value, type) => Convert.ToDateTime (value) },
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
				Type dictKeyType = genericTypes?.GetValue (0) as Type ?? typeof(object);
				Type dictValueType = genericTypes?.GetValue (1) as Type ?? typeof(object);
				foreach(var key in valueDict.Keys)
				{
					var newKey = dictKeyType != typeof(object) ? ValueConversion(key, dictKeyType) : key;
					var dictCurrentValue = valueDict[key];
					var newValue = dictValueType != typeof(object) ? ValueConversion(dictCurrentValue, dictValueType) : dictCurrentValue;
					newDict.Add(newKey, newValue);
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
				Type genericType = genericTypes?.GetValue(0) as Type ?? typeof(object);

				while (valueEnum.MoveNext ())
					newList.Add (ValueConversion (valueEnum.Current, genericType));

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
						temp.Add(ValueConversion(i, elementType));
					Array arr = Array.CreateInstance(elementType, temp.Count);
					Array.Copy(temp.ToArray (), arr, temp.Count);
					return arr;
				}
				else if (value.GetType ().IsArray)
				{
					Type elementType = type.GetElementType();
					Array valueArr = value as Array;
					Array arr = Array.CreateInstance(elementType, valueArr.Length);
					for (int i = 0; i < arr.Length; ++i)
						arr.SetValue(ValueConversion(valueArr.GetValue(i), elementType), i);
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
		};
		public static object ValueConversion (object value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException ();
			if (value == null || value.GetType () == type)
				return value;
			else
			{
				foreach (var kv in ValueConverters)
				{
					if (kv.Key (type))
						return kv.Value (value, type);
				}

				return FromObject (value.GetType (), value).ToObject (type);
			}
		}

		public string SectionName { get; set; }

		readonly List<ObjectKeyValue> objs = new List<ObjectKeyValue> ();

		public BlockarObject ()
		{

		}

		public BlockarObject (IDictionary<string, object> dict)
		{
			foreach (var kv in dict)
				Set (kv.Key, kv.Value);
		}

		public bool ContainsKey (string key)
		{
			foreach (var obj in objs)
			{
				if (obj.Key.Equals (key))
					return true;
			}
			return false;
		}

		public object Get (string key)
		{
			foreach (var obj in objs)
			{
				if (obj.Key.Equals (key))
					return obj.Value;
			}
			throw new KeyNotFoundException ();
		}
		public T Get<T> (string key)
		{
			return (T) Get (key, typeof (T));
		}
		public object Get (string key, Type type)
		{
			foreach (var obj in objs)
			{
				if (obj.Key.Equals (key))
				{
					var value = obj.Value;
					var valueType = value.GetType ();
					return ValueConversion (value, valueType);
				}
			}
			throw new KeyNotFoundException ();
		}

		public void Set (string key, object value)
		{
			foreach (var obj in objs)
			{
				if (obj.Key.Equals (key))
				{
					obj.Value = value;
					return;
				}
			}
			objs.Add (new ObjectKeyValue (key, value));
		}
		public void Set<T> (string key, object value)
		{
			Set (key, value);
		}

		public T Remove<T> (string key)
		{
			foreach (var obj in objs)
			{
				if (obj.Key.Equals (key))
				{
					objs.Remove (obj);
					return (T) obj.Value;
				}
			}
			throw new KeyNotFoundException ();
		}

		public void Clear () => objs.Clear ();

		public int Count => objs.Count;

		public T ToObject<T> () => (T) ToObject (typeof (T));

		public object ToObject (Type type)
		{
			object o = Activator.CreateInstance (type);

			if (o is ICustomObjectConverter)
			{
				(o as ICustomObjectConverter).FromBlockarObject (this);
				return o;
			}

			foreach (var member in type.GetMembers ())
			{
				if (!(member.MemberType == MemberTypes.Property
					|| member.MemberType == MemberTypes.Field))
					continue;

				if (member.GetCustomAttributes (typeof (NonSerializedAttribute), true)?.Length > 0)
					continue;

				var attrs = member.GetCustomAttributes (typeof (FieldOptionAttribute), true);
				var fieldOption = (attrs != null && attrs.Length > 0)
					? attrs [0] as FieldOptionAttribute
					: null;

				string name = fieldOption?.Name ?? member.Name;
#if !NET20
				if (member.MemberType == MemberTypes.Property)
				{
					PropertyInfo propInfo = member as PropertyInfo;
					propInfo.SetValue (o, Get (name, propInfo.PropertyType));
				}
				else
#endif
				{
					FieldInfo fieldInfo = member as FieldInfo;
					if (type.IsValueType)
						fieldInfo.SetValueDirect (__makeref(o), Get (name, fieldInfo.FieldType));
					else
						fieldInfo.SetValue (o, Get (name, fieldInfo.FieldType));
				}
			}

			return o;
		}

		public Dictionary<string, object> ToDictionary ()
		{
			Dictionary<string, object> obj = new Dictionary<string, object> ();

			foreach (var i in objs)
				obj.Add (i.Key, i.Value);

			return obj;
		}

		public static BlockarObject FromObject<T> (T obj) => FromObject (typeof (T), obj);

		public static BlockarObject FromObject (Type type, object obj)
		{
			var bo = new BlockarObject ();

			if (obj is ICustomObjectConverter)
			{
				(obj as ICustomObjectConverter).ToBlockarObject (bo);
				return bo;
			}

			var sectionNameAttr = type.GetCustomAttributes (typeof (SectionNameAttribute), true);
			if (sectionNameAttr != null && sectionNameAttr.Length > 0 && sectionNameAttr.GetValue (0) is SectionNameAttribute sectionName)
				bo.SectionName = sectionName.Name;

			foreach (var member in type.GetMembers ())
			{
				if (!(member.MemberType == MemberTypes.Property
					|| member.MemberType == MemberTypes.Field))
					continue;

				if (member.GetCustomAttributes (typeof (NonSerializedAttribute), true)?.Length > 0)
					continue;

				var attrs = member.GetCustomAttributes (typeof (FieldOptionAttribute), true);
				var fieldOption = (attrs != null && attrs.Length > 0)
					? attrs [0] as FieldOptionAttribute
					: null;

				string name = fieldOption?.Name ?? member.Name;
#if !NET20
				if (member.MemberType == MemberTypes.Property)
					bo.Set (name, (member as PropertyInfo).GetValue (obj));
				else
#endif
				{
					bo.Set (name, (member as FieldInfo).GetValue (obj));
				}
			}

			return bo;
		}

		public static BlockarObject FromDictionary (IDictionary<string, object> obj)
		{
			return new BlockarObject (obj);
		}

		public IEnumerator<ObjectKeyValue> GetEnumerator () => objs.GetEnumerator ();
		IEnumerator IEnumerable.GetEnumerator () => objs.GetEnumerator ();

		IEnumerator<string> IEnumerable<string>.GetEnumerator () { foreach (var obj in objs) yield return obj.Key; }

		IEnumerator<object> IEnumerable<object>.GetEnumerator () { foreach (var obj in objs) yield return obj.Value; }

		public void CopyFrom (BlockarObject obj)
		{
			Clear ();
			foreach (var kv in obj.objs)
				objs.Add (new ObjectKeyValue (kv));
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			builder.Append ('{');
			foreach (var kv in objs)
				builder.Append (kv.Key).Append (':').Append (kv.Value).Append (',');
			if (builder.Length > 1)
				builder.Remove (builder.Length - 1, 1);
			builder.Append ('}');
			return builder.ToString ();
		}
	}
}
