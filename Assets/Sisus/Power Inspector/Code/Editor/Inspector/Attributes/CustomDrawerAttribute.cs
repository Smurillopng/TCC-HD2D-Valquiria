using System;

namespace Sisus
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class CustomDrawerAttribute : Attribute
	{
		public readonly Type drawerType;
		public readonly object[] parameters;
		
		public CustomDrawerAttribute(Type setDrawerType)
		{
			drawerType = setDrawerType;
			parameters = null;
		}
		
		public CustomDrawerAttribute(Type setDrawerType, params object[] setParameters)
		{
			drawerType = setDrawerType;
			parameters = setParameters;
		}
	}
}