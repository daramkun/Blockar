using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Daramkun.Blockar.Json
{
	public enum ContainType { Unknown, Object, Array }

	public sealed class JsonContainer : IJsonContainer
	{
		internal Dictionary<object, object> container = new Dictionary<object, object> ();

		public ContainType ContainerType { get; private set; }

		public JsonContainer ( ContainType containerType ) { ContainerType = containerType; }
		public JsonContainer ( Stream stream ) { FromJsonContainer ( JsonParser.Parse ( stream ) ); }

		public void Add ( object value, object key = null )
		{
			if ( ContainerType == ContainType.Object && key == null )
				throw new ArgumentNullException ( "If ContainType is Object then key must have any value." );
			else if ( ContainerType == ContainType.Object && key != null && key.GetType () != typeof ( string ) )
				throw new ArgumentException ( "If ContainType is Object then key type must be the 'string'." );
			else if ( ContainerType == ContainType.Array && key != null && key.GetType () != typeof ( int ) )
				throw new ArgumentException ( "If ContainType is Array then key type must be the 'int'." );
			else
			{
				switch ( ContainerType )
				{
					case ContainType.Object:
						container.Add ( key, value );
						break;
					case ContainType.Array:
						container.Add ( key ?? container.Count, value );
						break;
				}
			}
		}

		public void Remove ( object key )
		{
			if ( ContainerType == ContainType.Object && key == null )
				throw new ArgumentNullException ( "key must have any value." );
			else if ( ContainerType == Json.ContainType.Object && key.GetType () != typeof ( string ) )
				throw new ArgumentException ( "If ContainType is Object then key type must be the 'string'." );
			else if ( ContainerType == Json.ContainType.Array && key.GetType () != typeof ( int ) )
				throw new ArgumentException ( "If ContainType is Array then key type must be the 'int'." );
			else
			{
				container.Remove ( key );
				if ( ContainerType == ContainType.Array )
				{
					for ( int i = ( int ) key; i < container.Count + 1; ++i )
						container.Add ( i, container [ i + 1 ] );
					container.Remove ( container.Count - 1 );
				}
			}
		}

		public object this [ object key ] { get { return container [ key ]; } set { container [ key ] = value; } }

		public bool Contains ( object key ) { return container.Keys.Contains ( key ); }

		public override string ToString ()
		{
			if ( container.Count == 0 ) return ( ContainerType == ContainType.Object ) ? "{ }" : "[ ]";

			StringBuilder text = new StringBuilder ();
			foreach ( KeyValuePair<object, object> record in container )
			{
				if ( ContainerType == ContainType.Object )
					text.AppendFormat ( "\"{0}\" : ", record.Key );
				if ( record.Value is string ) text.AppendFormat ( "\"{0}\", ", record.Value );
				else if ( record.Value is bool ) text.AppendFormat ( "{0}, ", ( ( bool ) record.Value ) ? "true" : "false" );
				else text.AppendFormat ( "{0}, ", record.Value ?? "null" );
			}
			text.Remove ( text.Length - 2, 2 );
			return string.Format ( (ContainerType == ContainType.Object) ? "{{ {0} }}" : "[ {0} ]", text );
		}

		public byte [] ToBinary ()
		{
			MemoryStream memoryStream = new MemoryStream ();
			BinaryWriter writer = new BinaryWriter ( memoryStream );
			writer.Write ( 0 );

			foreach ( KeyValuePair<object, object> record in container )
			{
				BSONType type;
				writer.Write ( ( byte ) ( type = GetValueType ( record.Value ) ) );
				writer.Write ( GetBinaryKey ( record.Key ) );
				switch ( type )
				{
					case BSONType.Document:
					case BSONType.Array:
						writer.Write ( ( record.Value as JsonContainer ).ToBinary () );
						break;
					case BSONType.Double:
						writer.Write ( ( double ) record.Value );
						break;
					case BSONType.Integer:
						writer.Write ( ( int ) record.Value );
						break;
					case BSONType.Integer64:
						writer.Write ( ( long ) record.Value );
						break;
					case BSONType.String:
					case BSONType.JavascriptCode:
						{
							byte [] data = Encoding.UTF8.GetBytes ( record.Value as string );
							writer.Write ( data.Length + 1 );
							writer.Write ( data );
							writer.Write ( ( byte ) 0 );
						}
						break;
					case BSONType.Regexp:
						{
							byte [] data = Encoding.UTF8.GetBytes ( (record.Value as Regex).ToString () );
							writer.Write ( data.Length + 1 );
							writer.Write ( data );
							writer.Write ( ( byte ) 0 );
						}
						break;
					case BSONType.UTCTime:
						writer.Write ( ( ( DateTime ) record.Value ).ToFileTimeUtc () );
						break;
					case BSONType.Boolean:
						writer.Write ( ( bool ) record.Value );
						break;
					case BSONType.BinaryData:
						{
							byte [] data = record.Value as byte [];
							writer.Write ( data.Length );
							writer.Write ( data );
						}
						break;
				}
			}

			return memoryStream.ToArray ();
		}

		private BSONType GetValueType ( object obj )
		{
			if(obj == null) return BSONType.Null;

			Type type = obj.GetType();

			if ( typeof ( double ) == type ) return BSONType.Double;
			else if ( typeof ( int ) == type ) return BSONType.Integer;
			else if ( typeof ( long ) == type ) return BSONType.Integer64;
			else if ( typeof ( string ) == type ) return BSONType.String;
			else if ( typeof ( DateTime ) == type ) return BSONType.UTCTime;
			else if ( typeof ( bool ) == type ) return BSONType.Boolean;
			else if ( typeof ( JsonContainer ) == type ) return ( obj as JsonContainer ).ContainerType == Json.ContainType.Object ? BSONType.Document : BSONType.Array;
			else if ( typeof ( Regex ) == type ) return BSONType.Regexp;
			else if ( typeof ( byte [] ) == type ) return BSONType.BinaryData;
			else throw new ArgumentException ( "There is unsupport data." );
		}

		private byte [] GetBinaryKey ( object key )
		{
			if ( key is int ) return new byte [] { ( byte ) ( object ) key };
			else if ( key is string )
			{
				MemoryStream s = new MemoryStream ();
				BinaryWriter w = new BinaryWriter ( s );
				w.Write ( ( key as string ).Length + 1 );
				w.Write ( Encoding.UTF8.GetBytes ( key as string ) );
				w.Write ( ( byte ) 0 );
				byte [] temp = s.ToArray ();
				s.Dispose ();
				return temp;
			}
			else throw new ArgumentException ( "key type must be 'int' or 'string'." );
		}

		public JsonContainer ToJsonContainer ()
		{
			return new JsonContainer ( ContainerType ) { container = new Dictionary<object, object> ( container ) };
		}

		public IJsonContainer FromJsonContainer ( JsonContainer container )
		{
			ContainerType = container.ContainerType;
			this.container = new Dictionary<object, object> ( container.container );
			return this;
		}

		public IEnumerable<object> GetListEnumerable () { return container.Values; }
		public IEnumerable<KeyValuePair<object, object>> GetDictionaryEnumerable () { return container; }
	}
}
