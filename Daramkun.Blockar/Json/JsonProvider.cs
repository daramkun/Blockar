using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Daramkun.Blockar.Json
{
	public static class JsonProvider
	{
		public static JsonContainer ToJsonContainer ( IJsonContainer container )
		{
			JsonContainer result = new JsonContainer ( container.ContainerType );
			
			if ( container.ContainerType == ContainType.Object )
			{
				PropertyInfo [] props = container.GetType ().GetProperties ();
				foreach ( PropertyInfo prop in props )
				{
					object [] attrs = prop.GetCustomAttributes ( typeof ( RecordAttribute ), true );
					foreach ( Attribute attr in attrs )
					{
						string name = ( attr as RecordAttribute ).Key;
						object data = prop.GetValue ( container, null );
						if ( name == null ) name = prop.Name;
						if ( data is IJsonContainer )
							data = ( data as IJsonContainer ).ToJsonContainer ();
						else if ( data is IEnumerable && !( data is string ) )
						{
							var temp = new JsonContainer ( ContainType.Array );
							foreach ( object d in data as IEnumerable )
								temp.Add ( d );
							data = temp;
						}
						result.Add ( data, name );
					}
				}

				FieldInfo [] fields = container.GetType ().GetFields ();
				foreach ( FieldInfo field in fields )
				{
					object [] attrs = field.GetCustomAttributes ( typeof ( RecordAttribute ), true );
					foreach ( Attribute attr in attrs )
					{
						string name = ( attr as RecordAttribute ).Key;
						object data = field.GetValue ( container );
						if ( name == null ) name = field.Name;
						if ( data is IJsonContainer )
							data = ( data as IJsonContainer ).ToJsonContainer ();
						else if ( data is IEnumerable && !( data is string ) )
						{
							var temp = new JsonContainer ( ContainType.Array );
							foreach ( object d in data as IEnumerable )
								temp.Add ( d );
							data = temp;
						}
						result.Add ( data, name );
					}
				}
			}
			else if ( container.ContainerType == ContainType.Array )
			{
				PropertyInfo [] props = container.GetType ().GetProperties ();
				foreach ( PropertyInfo prop in props )
				{
					object [] attrs = prop.GetCustomAttributes ( typeof ( InnerArrayAttribute ), true );
					InnerArrayAttribute attr = attrs [ 0 ] as InnerArrayAttribute;
					object data = prop.GetValue ( container, null );
					if ( data is IList )
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

			return result;
		}
	}
}
