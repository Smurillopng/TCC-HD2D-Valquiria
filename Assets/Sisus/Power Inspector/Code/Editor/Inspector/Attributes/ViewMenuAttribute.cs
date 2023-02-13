using System;

namespace Sisus
{
	// Class that allows exposing a static method, field or a property in the Power Inspector toolbar's view menu.
	// Perhaps could even work for instance class members, in which case they are only shown if found in inspected targets?
	// For static methods works quite similarly to the built-in MenuItemAttribute.
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class ViewMenuAttribute : Attribute
	{
		public readonly string menuPath;
		public readonly bool isValidate;
	
		public ViewMenuAttribute(string setMenuPath, bool setIsValidate = false)
		{
			menuPath = setMenuPath;
			isValidate = setIsValidate;
		}
	}
}