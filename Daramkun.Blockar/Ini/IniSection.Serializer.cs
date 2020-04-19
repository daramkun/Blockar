using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Ini
{
	partial class IniSection
	{
		private void Serialize_AddData ( string key, object i )
		{
			if ( i == null ) Add ( null, key );
			else if ( i is sbyte || i is byte || i is short || i is ushort || i is int || i is uint ||
				i is long || i is ulong || i is bool || i is float || i is double || i is string )
				Add ( key, i );
			else if ( i is char || i is TimeSpan || i is DateTime || i is Regex || i is Enum )
				Add ( i.ToString (), key );
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
			else return null;
		}

		public IniSection ( object obj, string name = null )
		{
			if ( obj == null )
				throw new ArgumentNullException ();

			Type type = obj.GetType ();
			Name = name ?? type.Name;

			if ( obj is IDictionary )
			{
				foreach ( DictionaryEntry i in obj as IDictionary )
					Serialize_AddData ( i.Key as string, i.Value );
			}
			else
			{
				foreach ( FieldInfo field in type.GetFields () )
				{
					object [] attrs = field.GetCustomAttributes ( typeof ( RecordAttribute ), false );
					if ( attrs.Length == 0 ) continue;
					RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
					Serialize_AddData ( attr.Name ?? field.Name, field.GetValue ( obj ) );
				}
				foreach ( PropertyInfo prop in type.GetProperties () )
				{
					object [] attrs = prop.GetCustomAttributes ( typeof ( RecordAttribute ), false );
					if ( attrs.Length == 0 ) continue;
					RecordAttribute attr = attrs.GetValue ( 0 ) as RecordAttribute;
					Serialize_AddData ( attr.Name ?? prop.Name, prop.GetValue ( obj, null ) );
				}	
			}
		}

		public T ToObject<T> ()
		{
			return ( T ) ToObject ( typeof ( T ) );	
		}

		private object ToObject ( Type type )
		{
			object obj = Activator.CreateInstance ( type );
			if ( obj is IDictionary )
			{
				foreach ( KeyValuePair<string, string> i in container )
				{
					( obj as IDictionary ).Add ( i.Key, null );
					PropertyInfo prop = type.GetProperty ( "Item" );
					prop.SetValue ( obj, Deserialize_GetData ( prop.PropertyType, i.Value ), new object [] { i.Key } );
				}
				return obj;
			}
			else
			{
				foreach ( KeyValuePair<string, string> i in this )
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
	}
}
