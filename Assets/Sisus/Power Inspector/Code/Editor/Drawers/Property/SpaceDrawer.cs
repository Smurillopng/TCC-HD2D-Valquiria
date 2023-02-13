using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary> Can be used to add some vertical spacing between drawers. </summary>
	[Serializable, DrawerForDecorator(typeof(SpaceAttribute), true), DrawerForDecorator(typeof(PSpaceAttribute), true)]
	public sealed class SpaceDrawer : BaseDrawer, IDecoratorDrawerDrawer
	{
		/// <summary> Amount of vertical space occupied by drawer. </summary>
		private float height;

		/// <inheritdoc/>
		public bool RequiresDecoratorDrawerType
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return height;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return Types.Void;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="height"> The height in pixels to occupy in the inspector. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static SpaceDrawer Create(float height, [CanBeNull]IParentDrawer parent)
		{
			SpaceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new SpaceDrawer();
			}
			result.Setup(height, parent);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public void SetupInterface(PropertyAttribute propertyAttribute, Type decoratorDrawerType, IParentDrawer setParent, LinkedMemberInfo attributeTarget)
		{
			var spaceAttribute = (SpaceAttribute)propertyAttribute;
			Setup(spaceAttribute.height, setParent);
		}

		/// <summary>
		/// Sets up the Drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setHeight"> The height in pixels to occupy in the inspector. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		private void Setup(float setHeight, IParentDrawer setParent)
		{
			height = setHeight;
			//base.Setup("", null, setParent, GUIContentPool.Create("SpaceAttribute"), true);
			base.Setup(setParent, GUIContentPool.Create("SpaceAttribute"));
		}

		/// <inheritdoc />
		protected override void Setup([CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return DrawGUI.MinPrefixLabelWidth;
		}

		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			return false;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				GetDrawPositions(position);
			}
			return false;
		}
	}
}