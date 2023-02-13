using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForDecorator(typeof(HeaderAttribute), true), DrawerForDecorator(typeof(PHeaderAttribute), true)]
	public sealed class HeaderDrawer : ReadOnlyTextDrawer, IDecoratorDrawerDrawer
	{
		private const float PreviousFieldOffset = 5f;
		private const float SingleLineHeight = 13f;
		private const float NextFieldOffset = 3f;

		private float height = PreviousFieldOffset + SingleLineHeight + NextFieldOffset;
		private float wordWrappedForWidth;

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

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="text"> The header text. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static HeaderDrawer Create(string text, [CanBeNull]IParentDrawer parent)
		{
			HeaderDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new HeaderDrawer();
			}
			result.Setup(text, parent);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other SetupInterface method.");
		}

		/// <inheritdoc />
		public void SetupInterface(PropertyAttribute propertyAttribute, Type decoratorDrawerType, IParentDrawer setParent, LinkedMemberInfo attributeTarget)
		{
			var header = (HeaderAttribute)propertyAttribute;
			Setup(header.header, setParent);
		}

		/// <inheritdoc />
		protected override void Setup(string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="text"> The header text. </param>
		/// <param name="setParent"> Drawer whose member this Drawer are. Can be null. </param>
		private void Setup(string text, IParentDrawer setParent)
		{
			wordWrappedForWidth = 0f;

			base.Setup(text, typeof(string), null, setParent, GUIContentPool.Create(text), true);
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return DrawGUI.MinPrefixLabelWidth;
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerUp" />
		public override IDrawer GetNextSelectableDrawerUp(int column, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerUp(column, this);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerDown" />
		public override IDrawer GetNextSelectableDrawerDown(int column, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerDown(column, this);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerLeft" />
		public override IDrawer GetNextSelectableDrawerLeft(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerLeft(moveToNextControlAfterReachingEnd, this);
		}

		/// <inheritdoc cref="IDrawer.GetNextSelectableDrawerRight" />
		public override IDrawer GetNextSelectableDrawerRight(bool moveToNextControlAfterReachingEnd, IDrawer requester)
		{
			return parent.GetNextSelectableDrawerRight(moveToNextControlAfterReachingEnd, this);
		}

		/// <inheritdoc cref="IDrawer.OnClick" />
		public override bool OnClick(Event inputEvent)
		{
			return false;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			switch(Event.current.type)
			{
				case EventType.Layout:
					GetDrawPositions(position);
					break;
				case EventType.Repaint:
					break;
				default:
					return false;
			}

			DrawGUI.Active.ColorRect(lastDrawPosition, DrawGUI.Active.InspectorBackgroundColor);
			GUI.Label(labelLastDrawPosition, label, InspectorPreferences.Styles.HeaderAttribute);
			return false;
		}

		/// <inheritdoc/>
		public override bool PassesSearchFilter(SearchFilter filter)
		{
			// I don't think it adds much value for the user to be able to search for headers, and they just end up adding clutter to the view.
			return false;
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			labelLastDrawPosition = position;
			DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);

			UpdateHeight(labelLastDrawPosition.width);
			
			lastDrawPosition = position;
			lastDrawPosition.height = height;

			labelLastDrawPosition.y += PreviousFieldOffset;
			labelLastDrawPosition.height = height - PreviousFieldOffset;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		private void UpdateHeight(float width)
		{
			if(wordWrappedForWidth != width)
			{
				wordWrappedForWidth = width;

				float setHeight = PreviousFieldOffset + InspectorPreferences.Styles.HeaderAttribute.CalcHeight(label, width) + NextFieldOffset;
				if(!height.Equals(setHeight))
				{
					height = setHeight;
					parent.OnChildLayoutChanged();
				}
			}
		}
	}
}