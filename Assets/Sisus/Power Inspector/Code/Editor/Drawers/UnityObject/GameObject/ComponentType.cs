using Sisus.Attributes;
using System;
using UnityEngine;

namespace Sisus
{	
	[Serializable]
	public class ComponentType
	{
		private Type type;

		[SerializeField, HideInInspector]
		private string typeSerialized = "";

		[ShowInInspector]
		public Type Type
		{
			get
			{
				if(type == null)
				{
					if(typeSerialized.Length > 0)
					{
						type = Type.GetType(typeSerialized);
					}
				}
				return type;
			}

			set
			{
				if(value == null || !typeof(Component).IsAssignableFrom(value))
				{
					type = null;
					typeSerialized = "";
				}
				else
				{
					type = value;
					typeSerialized = value.AssemblyQualifiedName;
				}
			}
		}

		public ComponentType() { }

		public ComponentType(Type componentType)
		{
			Type = componentType;
		}

		public static implicit operator Type(ComponentType componentType)
		{
			return componentType.Type;
		}
	}
}