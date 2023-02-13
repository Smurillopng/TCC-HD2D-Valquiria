using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class ReadOnlyTextDrawer : PrefixControlComboDrawer<string>
	{
		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(string);
			}
		}

		/// <inheritdoc/>
		public override bool SetValue(object newValue)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(string setValue, bool applyToField, bool updateMembers)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			return Value;
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ReadOnlyTextDrawer Create(string value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label = null)
		{
			ReadOnlyTextDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ReadOnlyTextDrawer();
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
		public override string DrawControlVisuals(Rect position, string inputValue)
		{
			if(inputValue == null)
			{
				bool guiWasEnabled = GUI.enabled;
				GUI.enabled = false;
				GUI.Label(position, "null");
				GUI.enabled = guiWasEnabled;

				return null;
			}

			GUI.Label(position, inputValue);
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
		protected override void Setup(string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
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
		protected override string GetRandomValue()
		{
			return Value;
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
	}
}