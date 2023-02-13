using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Interface for attributes that are used for grouping members.
	/// </summary>
	public interface IGroupAttribute
	{
		/// <summary> Prefix label for group foldout. </summary>
		GUIContent Label
		{
			get;
		}

		/// <summary> Create new instance of drawer that implements ICustomGroupDrawer and should be used for drawing the group. </summary>
		/// <param name="drawerByNameProvider"> Can provide a drawer type by its full or short name. </param>
		/// <returns> Drawer </returns>
		[NotNull]
		Type GetDrawerType(IDrawerByNameProvider drawerByNameProvider);
	}
}