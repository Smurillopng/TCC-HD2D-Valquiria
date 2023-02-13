using System;
using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class Converter
	{
		/// <summary>
		/// Attempts to change subject's type to such that it can be assigned to a field of the given type
		/// </summary>
		/// <param name="subject">
		/// [in,out] The subject. This may be null. </param>
		/// <param name="type">
		/// The type to which subject should be converted to. </param>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		public static bool TryChangeType([CanBeNull]ref object subject, Type type)
		{
			#if DEV_MODE
			Debug.Log("Converter.TryChangeType of " + StringUtils.ToString(subject) + " from " + StringUtils.TypeToString(subject)+" to "+ StringUtils.ToString(type));
			#endif

			if(subject == null)
			{
				subject = type.DefaultValue();
				return true;
			}

			var typeWas = subject.GetType();

			if(type.IsAssignableFrom(typeWas))
			{
				return true;
			}

			var convertible = subject as IConvertible;
			if(convertible != null)
			{
				if(typeof(IConvertible).IsAssignableFrom(type))
				{
					try
					{
						subject = Convert.ChangeType(subject, type);
					}
					catch
					{
						if(type.IsEnum && typeWas == Types.String)
						{
							subject = TypeExtensions.GetEnumType(subject as string);
						}
						return false;
					}
					return true;
				}
			}

			if(type.IsArray)
			{
				var elementType = type.GetElementType();
				var array = Array.CreateInstance(elementType, 0);
				var ienumerable = subject as IEnumerable;
				if(ienumerable == null)
				{
					return false;
				}

				int index = 0;
				foreach(var item in ienumerable)
				{
					object setItem = item;
					if(!TryChangeType(ref setItem, elementType))
					{
						return false;
					}

					try
					{
						array = array.InsertAt(index, setItem);
					}
					catch
					{
						return false;
					}
					
					index++;
				}
				subject = array;
				return true;
			}

			if(type.IsAbstract)
			{
				return false;
			}

			if(type.IsUnityObject())
			{
				var comp = subject as Component;
				if(comp != null)
				{
					if(type.IsGameObject())
					{
						subject = comp.gameObject;
						return true;
					}
					return false;
				}
				var go = subject as GameObject;
				if(go != null)
				{
					if(type.IsComponent())
					{
						subject = go.GetComponent<Type>();
						return true;
					}
					return false;
				}
				return false;
			}
			
			if(typeWas == Types.Color)
			{
				if(type == Types.Color32)
				{
					subject = (Color32)(Color)subject;
					return true;
				}
				return false;
			}
			if(typeWas == Types.Color32)
			{
				if(type == Types.Color)
				{
					subject = (Color)(Color32)subject;
					return true;
				}
				return false;
			}

			object set;
			try
			{
				set = Activator.CreateInstance(type);
			}
			catch
			{
				return false;
			}

			var currentFields = typeWas.GetFields(BindingFlags.Instance);
			var typeFields = type.GetFields(BindingFlags.Instance);
			
			for(int n = Mathf.Min(currentFields.Length, typeFields.Length) - 1; n >= 0; n--)
			{
				var fieldValue = currentFields[n].GetValue(subject);
				try
				{
					typeFields[n].SetValue(set, fieldValue);
				}
				catch
				{
					return false;
				}
			}
			subject = set;
			return true;
		}
	}
}