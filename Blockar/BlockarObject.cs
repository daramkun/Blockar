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

	public sealed partial class BlockarObject : IEnumerable<ObjectKeyValue>, IEnumerable<string>, IEnumerable<object>
	{
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
					return ValueConverter.ValueConvert (value, valueType);
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
#if !NET20 && !NET35
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
#if !NET20 && !NET35
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
