using System;
using System.ComponentModel;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class AddComponentMenuItemConfig
	{
		public string label;
		private Type type;
		
		[SerializeField, HideInInspector]
		private string typeSerialized = "";
		
		[EditorBrowsable]
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
				type = value;
				if(type == null)
				{
					typeSerialized = "";
				}
				else
				{
					typeSerialized = type.AssemblyQualifiedName;
				}
			}
		}

		public AddComponentMenuItemConfig()
		{
			
		}

		public AddComponentMenuItemConfig(string setLabel, Type setType)
		{
			label = setLabel;
			Type = setType;
		}
		
		public override string ToString()
		{
			return label;
		}
	}
}