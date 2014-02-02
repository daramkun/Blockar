using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Json
{
	public static class JsonProvider
	{
		private static void ToJsonContainer_Inner_Object ( IJsonContainer container, JsonContainer result, object [] f )
		{
			foreach ( var p in f )
			{
				object [] attrs = null;
				if ( p is PropertyInfo ) attrs = ( p as PropertyInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				else attrs = ( p as FieldInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				foreach ( Attribute attr in attrs )
				{
					string name = ( attr as RecordAttribute ).Key;
					object data = null;
					if ( p is PropertyInfo ) data = ( p as PropertyInfo ).GetValue ( container, null );
					else data = ( p as FieldInfo ).GetValue ( container );
					if ( name == null )
					{
						if ( p is PropertyInfo ) name = ( p as PropertyInfo ).Name;
						else name = ( p as FieldInfo ).Name;
					}
					if ( data is IJsonContainer ) data = ( data as IJsonContainer ).ToJsonContainer ();
					else if ( data is IEnumerable && !( data is string ) )
					{
						var temp = new JsonContainer ( ContainType.Array );
						foreach ( object d in data as IEnumerable ) temp.Add ( d );
						data = temp;
					}
					result.Add ( data, name );
				}
			}
		}

		private static void ToJsonContainer_Inner_Array ( IJsonContainer container, JsonContainer result, object [] f )
		{
			foreach ( var p in f )
			{
				object [] attrs = null;
				if ( p is PropertyInfo ) attrs = ( p as PropertyInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				else attrs = ( p as FieldInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				InnerArrayAttribute attr = attrs [ 0 ] as InnerArrayAttribute;
				object data = null;
				if ( p is PropertyInfo ) data = ( p as PropertyInfo ).GetValue ( container, null );
				else data = ( p as FieldInfo ).GetValue ( container );
				if ( ( data as PropertyInfo ).DeclaringType is IList )
				{
					IEnumerator i = ( data as IList ).GetEnumerator ();
					while ( i.MoveNext () )
						result.Add ( i.Current );
				}
				else if ( data is IEnumerable && !( data is string ) )
					foreach ( object i in data as IEnumerable )
						result.Add ( i );
				else if ( data is Dictionary<object, object> )
					result.container = new Dictionary<object, object> ( data as Dictionary<object, object> );
				break;
			}
		}

		public static JsonContainer ToJsonContainer ( IJsonContainer container )
		{
			JsonContainer result = new JsonContainer ( container.ContainerType );
			PropertyInfo [] props = container.GetType ().GetProperties ();
			FieldInfo [] fields = container.GetType ().GetFields ();
			
			if ( container.ContainerType == ContainType.Object )
			{
				ToJsonContainer_Inner_Object ( container, result, props );
				ToJsonContainer_Inner_Object ( container, result, fields );
			}
			else if ( container.ContainerType == ContainType.Array )
			{
				ToJsonContainer_Inner_Array ( container, result, props );
				ToJsonContainer_Inner_Array ( container, result, fields );
			}

			return result;
		}

		private static void FromJsonContainer_Inner_Object ( IJsonContainer container, JsonContainer data, object [] f )
		{
			foreach ( var p in f )
			{
				object [] attrs = null;
				if ( p is PropertyInfo ) attrs = ( p as PropertyInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				else attrs = ( p as FieldInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				foreach ( Attribute attr in attrs )
				{
					string name = ( attr as RecordAttribute ).Key;
					if ( name == null )
					{
						if ( p is PropertyInfo ) name = ( p as PropertyInfo ).Name;
						else name = ( p as FieldInfo ).Name;
					}
					if ( p is PropertyInfo ) ( p as PropertyInfo ).SetValue ( container, data [ name ], null );
					else ( p as FieldInfo ).SetValue ( container, data [ name ] );
				}
			}
		}

		private static void FromJsonContainer_Inner_Array ( IJsonContainer container, JsonContainer data, object [] f )
		{
			foreach ( var p in f )
			{
				object [] attrs = null;
				if ( p is PropertyInfo ) attrs = ( p as PropertyInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				else attrs = ( p as FieldInfo ).GetCustomAttributes ( typeof ( RecordAttribute ), true );
				foreach ( Attribute attr in attrs )
				{
					string name = ( attr as RecordAttribute ).Key;
					if ( name == null )
					{
						if ( p is PropertyInfo ) name = ( p as PropertyInfo ).Name;
						else name = ( p as FieldInfo ).Name;
					}

					Type type = null;
					if ( p is PropertyInfo ) type = ( p as PropertyInfo ).PropertyType;
					else type = ( p as FieldInfo ).FieldType;

					object d = null;
					if ( type.GetInterfaces ().Contains ( typeof ( IEnumerable ) ) )
					{
						List<object> list = new List<object> ();
						foreach ( object o in data.GetListEnumerable () )
							list.Add ( o );
						d = list.ToArray ();
					}

					if ( p is PropertyInfo ) ( p as PropertyInfo ).SetValue ( container, d, null );
					else ( p as FieldInfo ).SetValue ( container, d );
				}
			}
		}

		public static void FromJsonContainer ( IJsonContainer container, JsonContainer data )
		{
			PropertyInfo [] props = container.GetType ().GetProperties ();
			FieldInfo [] fields = container.GetType ().GetFields ();

			if ( container.ContainerType != data.ContainerType )
				throw new ArgumentException ();

			if ( container.ContainerType == ContainType.Object )
			{
				FromJsonContainer_Inner_Object ( container, data, props );
				FromJsonContainer_Inner_Object ( container, data, fields );
			}
			else if ( container.ContainerType == ContainType.Array )
			{
				FromJsonContainer_Inner_Array ( container, data, props );
				FromJsonContainer_Inner_Array ( container, data, fields );
			}
		}
	}
}
