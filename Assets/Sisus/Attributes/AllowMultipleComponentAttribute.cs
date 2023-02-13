using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// This is the opposite of the built-in DisallowMultipleComponent attribute.
	/// 
	/// If Power Inspector has been configured to disallow multiple instances of
	/// the same component type by default, then adding this attribute before
	/// specific component classes allows one to bypass that restrictions.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class AllowMultipleComponentAttribute : Attribute
	{

	}
}