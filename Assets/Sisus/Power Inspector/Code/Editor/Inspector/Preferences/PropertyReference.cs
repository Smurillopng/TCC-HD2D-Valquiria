using System;
using System.Reflection;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable, DrawInSingleRow]
	public class PropertyReference
	{
		[SerializeField, HideInInspector]
		public string ownerTypeName = "";

		[SerializeField, HideInInspector]
		public string propertyName = "";

		[NonSerialized]
		private PropertyInfo propertyInfo;

		[NonSerialized]
		private bool setupDone;

		[ShowInInspector, NotNull]
		public PropertyInfo Property
		{
			get
			{
				if(!setupDone)
				{
					UpdateTypeAndProperty();
				}
				return propertyInfo;
			}

			set
			{
				if(propertyInfo != value)
				{
					propertyInfo = value;
					if(value == null)
					{
						ownerTypeName = "";
						propertyName = "";
					}
					else
					{
						ownerTypeName = propertyInfo.ReflectedType.FullName;
						propertyName = propertyInfo.Name;
					}
					UpdateTypeAndProperty();
				}
			}
		}

		private void UpdateTypeAndProperty()
		{
			setupDone = true;

			propertyInfo = null;

			if(!string.IsNullOrEmpty(ownerTypeName))
			{
				var ownerType = TypeExtensions.GetType(ownerTypeName);
				if(ownerType != null && !string.IsNullOrEmpty(propertyName))
				{
					propertyInfo = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
					if(propertyInfo != null)
					{
						return;
					}

					for(var baseType = ownerType.BaseType; baseType != Types.SystemObject && baseType != null; baseType = baseType.BaseType)
					{
						propertyInfo = baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
						if(propertyInfo != null)
						{
							return;
						}
					}
				}
			}
		}
	}
}