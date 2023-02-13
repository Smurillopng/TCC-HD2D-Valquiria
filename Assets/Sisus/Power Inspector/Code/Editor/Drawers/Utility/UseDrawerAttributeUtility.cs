using System;
using System.Reflection;
using Sisus.Attributes;

namespace Sisus
{
	public static class UseDrawerAttributeUtility
	{
		public static bool TryGetCustomDrawerForClass(IDrawerProvider drawerProvider, Type classType, out Type drawerType)
		{
			IUseDrawer useDrawerAttribute;
			if(AttributeUtility.TryGetImplementingAttribute(classType, out useDrawerAttribute))
			{
				drawerType = useDrawerAttribute.GetDrawerType(classType, drawerProvider.GetClassDrawerType(classType, true), drawerProvider);

				#if DEV_MODE && PI_ASSERTATIONS
				UnityEngine.Debug.Assert(drawerType != null);
				UnityEngine.Debug.Assert(typeof(IUnityObjectDrawer).IsAssignableFrom(drawerType));
				#endif

				return true;
			}
			drawerType = null;
			return false;
		}

		public static bool TryGetCustomDrawerForClassMember(IDrawerProvider drawerProvider, MemberInfo member, out Type drawerType)
		{
			IUseDrawer useDrawerAttribute;
			if(AttributeUtility.TryGetImplementingAttribute(member, out useDrawerAttribute))
			{
				var declaringType = member.DeclaringType;
				drawerType = useDrawerAttribute.GetDrawerType(declaringType, drawerProvider.GetClassMemberDrawerType(declaringType, true), drawerProvider);

				#if DEV_MODE && PI_ASSERTATIONS
				UnityEngine.Debug.Assert(drawerType != null);
				UnityEngine.Debug.Assert(!typeof(IUnityObjectDrawer).IsAssignableFrom(drawerType));
				#endif

				return true;
			}
			drawerType = null;
			return false;
		}
	}
}