using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForDecorator(typeof(HelpBoxAttribute), true)]
	public sealed class HelpBoxDrawer : DynamicDecoratorDrawerDrawer
	{
		private const float PreviousFieldOffset = 5f;
		private const float SingleLineHeight = 13f;
		private const float NextFieldOffset = 3f;
		private const float IconWidth = 53f;

		private float height = PreviousFieldOffset + SingleLineHeight + NextFieldOffset;
		private float minHeight;
		private float wordWrappedForWidth;

		private IShowInInspectorIf showHelpBoxEvaluator;
		private MessageType messageType;

		/// <inheritdoc/>
		public override bool PrefixResizingEnabledOverControl
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override bool RequiresDecoratorDrawerType
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
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other SetupInterface method.");
		}

		/// <inheritdoc />
		public override void SetupInterface(PropertyAttribute propertyAttribute, Type decoratorDrawerType, IParentDrawer setParent, LinkedMemberInfo attributeTarget)
		{
			var helpBoxAttribute = (HelpBoxAttribute)propertyAttribute;
			Setup(helpBoxAttribute.text, attributeTarget, setParent, helpBoxAttribute.messageType, helpBoxAttribute.GetEvaluator(), helpBoxAttribute.minHeight);
		}

		/// <inheritdoc />
		protected override void Setup(string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		private void Setup(string text, [CanBeNull]LinkedMemberInfo attributeTarget, [CanBeNull]IParentDrawer setParent, HelpBoxMessageType setMessageType = HelpBoxMessageType.Info, IShowInInspectorIf showInInspectorEvaluator = null, float setMinHeight = 31f)
		{
			showHelpBoxEvaluator = showInInspectorEvaluator;
			minHeight = setMinHeight + PreviousFieldOffset + NextFieldOffset;

			switch(setMessageType)
			{
				case HelpBoxMessageType.Info:
					messageType = MessageType.Info;
					break;
				case HelpBoxMessageType.Warning:
					messageType = MessageType.Warning;
					break;
				case HelpBoxMessageType.Error:
					messageType = MessageType.Error;
					break;
				default:
					throw new IndexOutOfRangeException();
			}
			
			base.Setup(text, typeof(string), attributeTarget, setParent, GUIContentPool.Create(text), false);

			float drawWidth = setParent != null ? setParent.Bounds.width : DrawGUI.InspectorWidth;
			UpdateHeight(drawWidth - IconWidth);
		}

		/// <inheritdoc />
		public override bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			return base.ShowInInspector(containingClassType, containingClassInstance, classMember) && showHelpBoxEvaluator == null ? true : showHelpBoxEvaluator.ShowInInspector(containingClassType, containingClassInstance, classMember);
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}

			DrawGUI.Active.ColorRect(position, DrawGUI.Active.InspectorBackgroundColor);

			DrawGUI.Active.HelpBox(labelLastDrawPosition, Value, messageType);
			return false;
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			labelLastDrawPosition = position;
			DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);

			UpdateHeight(labelLastDrawPosition.width - IconWidth);
			
			lastDrawPosition = position;
			lastDrawPosition.height = height;

			labelLastDrawPosition.y += PreviousFieldOffset;
			labelLastDrawPosition.height = height - PreviousFieldOffset - NextFieldOffset;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		private void UpdateHeight(float width)
		{
			if(wordWrappedForWidth != width)
			{
				wordWrappedForWidth = width;

				float setHeight = PreviousFieldOffset + InspectorPreferences.Styles.HelpBox.CalcHeight(label, width) + NextFieldOffset;

				if(setHeight < minHeight)
				{
					setHeight = minHeight;
				}

				if(!height.Equals(setHeight))
				{
					height = setHeight;
					parent.OnChildLayoutChanged();
				}
			}
		}
	}
}