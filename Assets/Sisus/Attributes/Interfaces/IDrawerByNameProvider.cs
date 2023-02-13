using System;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Interface for classes that are able to provide a drawer type by its name.
	/// </summary>
	public interface IDrawerByNameProvider
	{
		/// <summary> Gets type of drawer based on the class name. </summary>
		/// <param name="drawerName"> Full name or short name of drawer class. </param>
		/// <returns> Type of drawer class that implements IComponentDrawer. Null if no such drawer was found. </returns>
		[CanBeNull]
		Type GetDrawerTypeByName([NotNull]string drawerName);

		/// <summary> Gets type of drawer for drawing data of components based on the class name. </summary>
		/// <param name="drawerName"> Full name or short name of drawer class. </param>
		/// <returns> Type of drawer class that implements IComponentDrawer. Null if no such drawer was found. </returns>
		[CanBeNull]
		Type GetComponentDrawerTypeByName([NotNull]string drawerName);

		/// <summary> Gets type of drawer for drawing data of fields based on the the class name. </summary>
		/// <param name="drawerName"> Full name or short name of drawer class. </param>
		/// <returns> Type of drawer class that implements IFieldDrawer. Null if no such drawer was found. </returns>
		[CanBeNull]
		Type GetFieldDrawerTypeByName([NotNull]string drawerName);
	}
}