using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Interface that an attribute can implement when it can be added to a target to
	/// specify the drawer that should be used when drawing the target in question in Power Inspector.
	/// </summary>
	public interface IUseDrawer
	{
		/// <summary>
		/// Gets parameters that can be used during Setup phase of a drawer that was selected using this attribute.
		/// </summary>
		/// <param name="attributeHolderType"> Type of field, property, method or class which contains the attribute. </param>
		/// <param name="defaultDrawerTypeForAttributeHolder">
		/// Default drawer type that would be used for attribute holder, had this attribute not been added to it.
		/// </param>
		/// <param name="drawerByNameProvider"> Can provide a drawer type by its full or short name. </param>
		/// <returns>
		/// Type of drawer to use for drawing the attribute holder in Power Inspector.
		/// </returns>
		[NotNull]
		Type GetDrawerType([NotNull]Type attributeHolderType, [NotNull]Type defaultDrawerTypeForAttributeHolder, [NotNull]IDrawerByNameProvider drawerByNameProvider);
	}
}