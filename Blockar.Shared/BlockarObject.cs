using System;
using System.CodeDom;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramee.Blockar
{
	public struct ObjectKeyValue
	{
		public readonly string Key;
		public object Value;

		public ObjectKeyValue(string key)
		{
			Key = key;
			Value = default;
		}

		public ObjectKeyValue(string key, object value = null)
		{
			Key = key;
			Value = value;
		}

		public ObjectKeyValue(ObjectKeyValue kv)
		{
			Key = kv.Key;
			Value = kv.Value;
		}

		public override string ToString() => $"{{{Key}:{Value}}}";

		public override bool Equals(object obj)
		{
			if (obj is ObjectKeyValue)
			{
				return Key == ((ObjectKeyValue) obj).Key;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Key.GetHashCode();
		}

		public static bool operator ==(ObjectKeyValue kv1, ObjectKeyValue kv2)
		{
			return kv1.Key == kv2.Key;
		}

		public static bool operator !=(ObjectKeyValue kv1, ObjectKeyValue kv2)
		{
			return kv1.Key != kv2.Key;
		}
	}

	public sealed partial class BlockarObject : IEnumerable<ObjectKeyValue>, IEnumerable<string>, IEnumerable<object>
	{
#if !NET35
		static ConcurrentQueue<List<ObjectKeyValue>> listPool = new ConcurrentQueue<List<ObjectKeyValue>>();
#endif

		public string SectionName { get; set; }

		readonly List<ObjectKeyValue> objs;

		public BlockarObject()
		{
#if !NET35
			if (!listPool.TryDequeue(out objs))
				objs = new List<ObjectKeyValue>(16);
#else
			objs = new List<ObjectKeyValue> (16);
#endif
		}

#if !NET35
		~BlockarObject()
		{
			listPool.Enqueue(objs);
		}
#endif

		public BlockarObject(IDictionary<string, object> dict)
		{
			foreach (var (key, value) in dict)
				Set(key, value);
		}

		public bool ContainsKey(string key)
		{
			return objs.Any(obj => obj.Key.Equals(key));
		}

		public int GetKeyIndex(string key)
		{
			for (var i = 0; i < objs.Count; ++i)
				if (objs[i].Key == key)
					return i;
			return -1;
		}

		public string GetIndexKey(int index)
		{
			return objs[index].Key;
		}

		public object Get(string key)
		{
			foreach (var obj in objs.Where(obj => obj.Key.Equals(key)))
			{
				return obj.Value;
			}

			throw new KeyNotFoundException();
		}

		public T Get<T>(string key)
		{
			return (T) Get(key, typeof(T));
		}

		public object Get(string key, Type type)
		{
			foreach (var value in from obj in objs
				where obj.Key.Equals(key)
				select obj.Value
				into value
				let valueType = value?.GetType()
				select value)
			{
				return ValueConverter.ValueConvert(value, type);
			}

			throw new KeyNotFoundException();
		}

		public void Set(string key, object value)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				if (!objs[i].Key.Equals(key))
					continue;

				objs[i] = new ObjectKeyValue(key, value);
				return;
			}

			if (objs.Count == objs.Capacity)
				objs.Capacity *= 2;
			objs.Add(new ObjectKeyValue(key, value));
		}

		public void Set<T>(string key, T value)
		{
			Set(key, (object) value);
		}

		public object this[int index]
		{
			get => objs[index].Value;
			set => objs[index] = new ObjectKeyValue(objs[index].Key, value);
		}

		public T Remove<T>(string key)
		{
			for (var i = 0; i < objs.Count; ++i)
			{
				if (!objs[i].Key.Equals(key))
					continue;

				var ret = objs[i].Value;
				objs.RemoveAt(i);
				return (T) ret;
			}

			throw new KeyNotFoundException();
		}

		public void Clear() => objs.Clear();

		public int Count => objs.Count;

		public T ToObject<T>() => (T) ToObject(typeof(T));

		private static IEnumerable<MemberInfo> GetFieldsAndProperties(Type type)
		{
			foreach (var member in type.GetMembers())
			{
				if (!(member.MemberType == MemberTypes.Property
				      || member.MemberType == MemberTypes.Field))
					continue;

				if (member.DeclaringType != type)
					continue;

				if ((member.MemberType == MemberTypes.Property &&
				     ((PropertyInfo) member).GetAccessors().Any(x => x.IsStatic)) ||
				    (member.MemberType == MemberTypes.Field && ((FieldInfo) member).IsStatic))
					continue;

				yield return member;
			}
		}

		public object ToObject(Type type)
		{
			var o = Activator.CreateInstance(type);

			if (o is ICustomObjectConverter converter)
			{
				converter.FromBlockarObject(this);
				return converter;
			}

			foreach (var member in GetFieldsAndProperties(type))
			{
				if (member.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0)
					continue;

				var attrs = member.GetCustomAttributes(typeof(FieldOptionAttribute), true);
				var fieldOption = (attrs.Length > 0)
					? attrs[0] as FieldOptionAttribute
					: null;

				var name = fieldOption?.Name ?? member.Name;
				if (!ContainsKey(name))
					continue;
#if !NET20 && !NET35
				if (member.MemberType == MemberTypes.Property)
				{
					var propInfo = member as PropertyInfo;
					propInfo?.SetValue(o, Get(name, propInfo.PropertyType));
				}
				else
#endif
				{
					var fieldInfo = member as FieldInfo;
					if (type.IsValueType)
						fieldInfo?.SetValueDirect(__makeref(o), Get(name, fieldInfo.FieldType));
					else
						fieldInfo?.SetValue(o, Get(name, fieldInfo.FieldType));
				}
			}

			return o;
		}

		public Dictionary<string, object> ToDictionary()
		{
			return objs.ToDictionary(i => i.Key, i => i.Value);
		}

		public static BlockarObject FromObject<T>(T obj) => FromObject(typeof(T), obj);

		public static BlockarObject FromObject(Type type, object obj)
		{
			if (obj is BlockarObject)
				return obj as BlockarObject;

			var bo = new BlockarObject();

			if (obj is ICustomObjectConverter converter)
			{
				converter.ToBlockarObject(bo);
				return bo;
			}

			var sectionNameAttr = type.GetCustomAttributes(typeof(SectionNameAttribute), true);
			if (sectionNameAttr.Length > 0 && sectionNameAttr.GetValue(0) is SectionNameAttribute sectionName)
				bo.SectionName = sectionName.Name;

			foreach (var member in GetFieldsAndProperties(type))
			{
				if (member.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0)
					continue;

				var attrs = member.GetCustomAttributes(typeof(FieldOptionAttribute), true);
				var fieldOption = (attrs.Length > 0)
					? attrs[0] as FieldOptionAttribute
					: null;

				var name = fieldOption?.Name ?? member.Name;
				var value = member.MemberType == MemberTypes.Property
						? (member as PropertyInfo)?.GetValue(obj, null)
						: (member as FieldInfo)?.GetValue(obj);
				if (!(value is byte || value is sbyte || value is short || value is ushort ||
					value is int || value is uint || value is long || value is ulong || value is float || value is double ||
					value is bool || value is string || value is DateTime || value is Regex || value is TimeSpan ||
					value is decimal || value is CultureInfo || value is char || value is Enum) && value != null)
					value = FromObject(value.GetType(), value);
				bo.Set(name, value);
			}

			return bo;
		}

		public static BlockarObject FromDictionary(IDictionary<string, object> obj)
		{
			return new BlockarObject(obj);
		}

		public IEnumerator<ObjectKeyValue> GetEnumerator() => objs.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => objs.GetEnumerator();

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return objs.Select(obj => obj.Key).GetEnumerator();
		}

		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			return objs.Select(obj => obj.Value).GetEnumerator();
		}

		public void CopyFrom(BlockarObject obj)
		{
			Clear();
			foreach (var kv in obj.objs)
				objs.Add(new ObjectKeyValue(kv));
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.Append('{');
			foreach (var kv in objs)
				builder.Append(kv.Key).Append(':').Append(kv.Value ?? "null").Append(',');
			if (builder.Length > 1)
				builder.Remove(builder.Length - 1, 1);
			builder.Append('}');
			return builder.ToString();
		}
	}
}
