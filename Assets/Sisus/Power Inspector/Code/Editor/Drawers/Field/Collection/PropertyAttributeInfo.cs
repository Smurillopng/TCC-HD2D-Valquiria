using System;
using UnityEngine;

namespace Sisus
{
	public class PropertyAttributeInfo
	{
		public PropertyAttribute attribute;
		public Type drawerType;
		
		public PropertyAttributeInfo(PropertyAttribute setAttribute, Type setDrawerType)
		{
			attribute = setAttribute;
			drawerType = setDrawerType;
		}
	}
}
