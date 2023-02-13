#if UNITY_EDITOR
using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	public abstract class DynamicDecoratorDrawerDrawer : ReadOnlyTextDrawer, IDecoratorDrawerDrawer, IShowInInspectorIf
	{
		/// <inheritdoc/>
		public abstract bool RequiresDecoratorDrawerType
		{
			get;
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		public override bool ReadOnly
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		public override bool CanReadFromFieldWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override bool CanWriteToFieldWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		public override bool Clickable
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other SetupInterface method.");
		}

		/// <inheritdoc />
		public abstract void SetupInterface(PropertyAttribute propertyAttribute, Type decoratorDrawerType, IParentDrawer setParent, LinkedMemberInfo attributeTarget);

		/// <inheritdoc />
		protected override void Setup(string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			showInInspectorIf = this;
			passedLastShowInInspectorIfTest = setParent == null || ShowInInspector(setParent.Type, setParent.GetValue(), setMemberInfo == null ? null : setMemberInfo.MemberInfo);

			#if DEV_MODE
			Debug.Log("passedLastShowInInspectorIfTest: "+ passedLastShowInInspectorIfTest+ ", ShouldShowInInspector=" + ShouldShowInInspector+ ", setParent="+ (setParent == null ? "null" : setParent.ToString()));
			#endif
		}

		/// <inheritdoc />
		public virtual bool ShowInInspector([NotNull]Type containingClassType, [CanBeNull]object containingClassInstance, [CanBeNull]MemberInfo classMember)
		{
			var targetedDrawer = DecoratorDrawerDrawer.GetTargetClassMemberDrawer(this);
			return targetedDrawer == null ? true : targetedDrawer.ShouldShowInInspector;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position) { return false; }

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

		/// <inheritdoc/>
		public override bool PassesSearchFilter(SearchFilter filter)
		{
			// Probably not useful for the user to be able to search for decorators, and they just end up adding clutter to the view.
			return false;
		}
	}
}
#endif