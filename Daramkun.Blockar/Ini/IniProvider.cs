using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Daramkun.Blockar.Common;

namespace Daramkun.Blockar.Ini
{
	public static class IniProvider
	{
		private static void ToIniSection_Inner ( IIniSection section, IniSection result, object [] f )
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
					object data = null;
					if ( p is PropertyInfo ) data = ( p as PropertyInfo ).GetValue ( section, null );
					else data = ( p as FieldInfo ).GetValue ( section );
					if ( name == null )
					{
						if ( p is PropertyInfo ) name = ( p as PropertyInfo ).Name;
						else name = ( p as FieldInfo ).Name;
					}
					result.Add ( name, data );
				}
			}
		}

		public static IniSection ToIniSection ( IIniSection section )
		{
			IniSection result = new IniSection ();
			result.Name = section.Name;

			PropertyInfo [] props = section.GetType ().GetProperties ();
			FieldInfo [] fields = section.GetType ().GetFields ();

			ToIniSection_Inner ( section, result, props );
			ToIniSection_Inner ( section, result, fields );

			return result;
		}

		private static void FromIniSection_Inner ( IIniSection section, IniSection data, object [] f )
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

					object d = data [ name ];

					if ( type == typeof ( string ) ) d = d as string;
					else if ( type == typeof ( int ) ) d = int.Parse ( d as string );
					else if ( type == typeof ( short ) ) d = short.Parse ( d as string );
					else if ( type == typeof ( long ) ) d = long.Parse ( d as string );
					else if ( type == typeof ( bool ) ) d = bool.Parse ( d as string );
					else if ( type == typeof ( float ) ) d = float.Parse ( d as string );
					else if ( type == typeof ( double ) ) d = double.Parse ( d as string );
					else if ( type == typeof ( byte ) ) d = byte.Parse ( d as string );
					else if ( type == typeof ( sbyte ) ) d = sbyte.Parse ( d as string );
					else if ( type == typeof ( ushort ) ) d = ushort.Parse ( d as string );
					else if ( type == typeof ( uint ) ) d = uint.Parse ( d as string );
					else if ( type == typeof ( ulong ) ) d = ulong.Parse ( d as string );
					else if ( type == typeof ( TimeSpan ) ) d = TimeSpan.Parse ( d as string );
					else if ( type == typeof ( DateTime ) ) d = DateTime.Parse ( d as string );


					if ( p is PropertyInfo ) ( p as PropertyInfo ).SetValue ( section, d, null );
					else ( p as FieldInfo ).SetValue ( section, d );
				}
			}
		}

		public static void FromIniSection ( IIniSection section, IniSection data )
		{
			section.Name = data.Name;

			PropertyInfo [] props = section.GetType ().GetProperties ();
			FieldInfo [] fields = section.GetType ().GetFields ();

			FromIniSection_Inner ( section, data, props );
			FromIniSection_Inner ( section, data, fields );
		}
	}
}
