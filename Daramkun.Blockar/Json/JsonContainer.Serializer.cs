using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Json
{
	partial class JsonContainer
	{
		private void Serialize_AddData ( object i, object key )
		{
			if ( i == null ) Add ( null, key );
			else if ( i is sbyte || i is byte || i is short || i is ushort || i is int || i is uint ||
				i is long || i is ulong || i is bool || i is float || i is double || i is string )
				Add ( i, key );
			else if ( i is char || i is TimeSpan || i is DateTime || i is Regex || i is Enum )
				Add ( i.ToString (), key );
			else Add ( new JsonContainer ( i ), key );
		}

		private object Deserialize_GetData ( Type fieldType, object obj )
		{
			if ( obj == null ) return null;
			if ( fieldType == typeof ( sbyte ) ) return Convert.ToSByte ( obj );
			else if ( fieldType == typeof ( byte ) ) return Convert.ToByte ( obj );
			else if ( fieldType == typeof ( short ) ) return Convert.ToInt16 ( obj );
			else if ( fieldType == typeof ( ushort ) ) return Convert.ToUInt16 ( obj );
			else if ( fieldType == typeof ( int ) ) return Convert.ToInt32 ( obj );
			else if ( fieldType == typeof ( uint ) ) return Convert.ToUInt32 ( obj );
			else if ( fieldType == typeof ( long ) ) return Convert.ToInt64 ( obj );
			else if ( fieldType == typeof ( ulong ) ) return Convert.ToUInt64 ( obj );
			else if ( fieldType == typeof ( bool ) ) return Convert.ToBoolean ( obj );
			else if ( fieldType == typeof ( float ) ) return Convert.ToSingle ( obj );
			else if ( fieldType == typeof ( double ) ) return Convert.ToDouble ( obj );
			else if ( fieldType == typeof ( string ) ) return Convert.ToString ( obj );
			else if ( fieldType == typeof ( char ) )
			{
				if ( obj is string ) return ( obj as string ) [ 0 ];
				else return Convert.ToChar ( obj );
			}
			else if ( fieldType == typeof ( TimeSpan ) ) return TimeSpan.Parse ( obj as string );
			else if ( fieldType == typeof ( DateTime ) ) return Convert.ToDateTime ( obj );
			else if ( fieldType == typeof ( Regex ) ) return new Regex ( obj as string );
			else if ( fieldType.IsSubclassOf ( typeof ( Enum ) ) || fieldType == typeof ( Enum ) )
				return Enum.Parse ( fieldType, obj as string, false );
			else if ( obj is JsonContainer ) return ( obj as JsonContainer ).ToObject ( fieldType );
			else return null;
		}

		public JsonContainer ( object obj )
		{
			if ( obj == null )
				throw new ArgumentNullException ();

			Type type = obj.GetType ();

			if ( obj is IList )
			{
				ContainerType = ContainType.Array;
				foreach ( object i in obj as IList )
				{
					Serialize_AddData ( i, null );
				}
			}
			else
			{
				ContainerType = ContainType.Object;
				if ( obj is IDictionary )
				{
					foreach ( DictionaryEntry i in obj as IDictionary )
						Serialize_AddData ( i.Value, i.Key.ToString () );
				}
				else
				{
					foreach ( FieldInfo field in type.GetFields () )
					{
						object [] attrs = field.GetCustomAttributes ( typeof ( RecordAttribute ), false );
						if ( attrs.Length == 0 ) continue;
						RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
						Serialize_AddData ( field.GetValue ( obj ), attr.Name ?? field.Name );
					}
					foreach ( PropertyInfo prop in type.GetProperties () )
					{
						object [] attrs = prop.GetCustomAttributes ( typeof ( RecordAttribute ), false );
						if ( attrs.Length == 0 ) continue;
						RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
						Serialize_AddData ( prop.GetValue ( obj, null ), attr.Name ?? prop.Name );
					}
				}
			}
		}

		public T ToObject<T> ()
		{
			return ( T ) ToObject ( typeof ( T ) );
		}

		public object ToObject ( Type type )
		{
			if ( ContainerType == ContainType.Object )
			{
				object obj = Activator.CreateInstance ( type );
				if ( obj is IDictionary )
				{
					foreach ( KeyValuePair<object, object> i in container )
					{
						( obj as IDictionary ).Add ( i.Key, null );
						PropertyInfo prop = type.GetProperty ( "Item" );
						prop.SetValue ( obj, Deserialize_GetData ( prop.PropertyType, i.Value ), new object [] { i.Key } );
					}
					return obj;
				}
				else
				{
					foreach ( KeyValuePair<object, object> i in GetDictionaryEnumerable () )
					{
						foreach ( FieldInfo field in type.GetFields () )
						{
							object [] attrs = field.GetCustomAttributes ( typeof ( RecordAttribute ), false );
							if ( attrs.Length == 0 ) continue;
							RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
							if ( ( attr.Name ?? field.Name ) == i.Key as string )
								field.SetValue ( obj, Deserialize_GetData ( field.FieldType, i.Value ) );
						}
						foreach ( PropertyInfo prop in type.GetProperties () )
						{
							object [] attrs = prop.GetCustomAttributes ( typeof ( RecordAttribute ), false );
							if ( attrs.Length == 0 ) continue;
							RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
							if ( ( attr.Name ?? prop.Name ) == i.Key as string )
								prop.SetValue ( obj, Deserialize_GetData ( prop.PropertyType, i.Value ), null );
						}
					}
				}
				return obj;
			}
			else if ( ContainerType == ContainType.Array )
			{
				if ( type.IsSubclassOf ( typeof ( Array ) ) )
				{
					object obj = Activator.CreateInstance ( type, container.Count );
					foreach ( KeyValuePair<object, object> i in container )
					{
						MethodInfo method = type.GetMethod ( "Set" );
						method.Invoke ( obj, new object [] { i.Key, i.Value } );
					}
					return obj;
				}
				else if ( type.IsSubclassOf ( typeof ( IList ) ) )
				{
					object obj = Activator.CreateInstance ( type );
					for ( int i = 0; i < container.Count; ++i )
						( obj as IList ).Add ( null );
					foreach ( KeyValuePair<object, object> i in container )
					{
						PropertyInfo prop = type.GetProperty ( "Item" );
						prop.SetValue ( obj, Deserialize_GetData ( prop.PropertyType, i.Value ), new object [] { i.Key } );
					}
					return obj;
				}
			}
			return null;
		}
	}
}
