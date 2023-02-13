using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public interface IShowInInspectorIf
	{
		/// <summary>
		/// Determines whether or not class member that follows attribute should be shown in the inspector at this time.
		/// </summary>
		/// <param name="containingClassType"> Type of class in which the class member target is. This can not be null. </param>
		/// <param name="containingClassInstance"> Instance of class that contains target member. Can be null for static class members. </param>
		/// <param name="classMember"> Instance of class that contains target member. Can be null for static class members. </param>
		/// <returns> True if target class member should currently be shown in the inspector. </returns>
		bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember);
	}
}