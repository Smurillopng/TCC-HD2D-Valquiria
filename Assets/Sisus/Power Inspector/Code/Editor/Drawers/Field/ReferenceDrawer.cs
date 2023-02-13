using System;
using UnityEngine;

namespace Sisus
{
	public sealed class ReferenceDrawer : PrefixControlComboDrawer<object>
	{
		private static readonly GUIContent buttonLabel = new GUIContent("Go To");

		/// <summary>
		/// GUIStyle for the button.
		/// </summary>
		private static GUIStyle Style
		{
			get
			{
				return InspectorPreferences.Styles.MiniButton;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				IDrawer drawer;
				if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
				{
					return drawer.Type;
				}
				return typeof(object);
			}
		}

		/// <inheritdoc/>
		public override bool SetValue(object newValue)
		{
			IDrawer drawer;
			if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
			{
				return drawer.SetValue(newValue);
			}
			return false;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(object setValue, bool applyToField, bool updateMembers)
		{
			IDrawer drawer;
			if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
			{
				return drawer.SetValue(setValue, applyToField, updateMembers);
			}
			return false;
		}

		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			IDrawer drawer;
			if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
			{
				return drawer.GetValue(index);
			}
			return Value;
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ReferenceDrawer Create(string value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label = null)
		{
			ReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ReferenceDrawer();
			}
			result.Setup(value, typeof(string), memberInfo, parent, label, true);
			result.LateSetup();
			return result;
		}

		private static void CantSetValueError() { Debug.LogError("ReadOnly value can't be changed"); }
		
		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <inheritdoc/>
		protected override void ApplyValueToField() { }

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void DoReset() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void OnValidate() { }

		/// <inheritdoc />
		public override object DrawControlVisuals(Rect position, object inputValue)
		{
			if(GUI.Button(position, buttonLabel, Style))
			{
				IDrawer drawer;
				if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
				{
					Inspector.Select(drawer, ReasonSelectionChanged.ControlClicked);
				}
			}

			return inputValue;
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			//no mouseover effects, since field is not editable
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			string setStringValue = setValue as string;
			if(setStringValue == null)
			{
				setStringValue = StringUtils.ToString(setValue);
			}
			Setup(setStringValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, true);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			ParentDrawerUtility.AddMenuItemsFromContextMenuAttribute(GetValues(), ref menu);
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc />
		protected override void DoRandomize()
		{
			IDrawer drawer;
			if(InspectorValues.IsDuplicateReferenceFor(MemberInfo, out drawer))
			{
				drawer.Randomize(false);
			}
		}

		/// <inheritdoc />
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc />
		protected override bool UpdateCachedValueFromField(bool updateMembers)
		{
			return true;
		}

		protected override object GetRandomValue()
		{
			return Type.DefaultValue();
		}
	}
}