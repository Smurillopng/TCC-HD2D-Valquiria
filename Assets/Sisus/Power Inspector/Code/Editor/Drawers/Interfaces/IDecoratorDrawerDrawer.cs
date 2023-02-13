using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public interface IDecoratorDrawerDrawer : IDrawer
	{
		/// <summary> Gets a value indicating whether the drawer requires a decorator drawer type to be provided when SetupInterface is called. </summary>
		/// <value> True if requires decorator drawer type, false if not. </value>
		bool RequiresDecoratorDrawerType
		{
			get;
		}

		/// <summary>
		/// Sets up the IDecoratorDrawerDrawer so that they're ready to be used.
		/// LateSetup should always be called right after this.
		/// </summary>
		/// <param name="propertyAttribute"> The PropertyAttribute whose DecoroatorDrawer the drawer represent. Can not be null. </param>
		/// <param name="decoratorDrawerType">
		/// The type of the DecoratorDrawer to use for drawing the field. Can be null for some drawer
		/// like HeaderDrawer, which makes it possible to support the drawer at runtime.
		/// With some drawer like DecoratorDrawerDrawer however, this is required.
		/// </param>
		/// <param name="setParent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="attributeTarget"> LinkedMemberInfo of class member which the attribute targets. </param>
		void SetupInterface([NotNull]PropertyAttribute propertyAttribute, [CanBeNull]Type decoratorDrawerType, [CanBeNull]IParentDrawer setParent, LinkedMemberInfo attributeTarget);
	}
}