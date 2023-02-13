#define DEBUG_NEXT_FIELD

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for a field that can be used to add new items to a collection such as a Dictionary of a HashSet.
	/// </summary>
	[Serializable]
	public sealed class CollectionAddFieldDrawer : ParentFieldDrawer<object>
	{
		private const float buttonOffset = 3f;
		private const float addButtonWidth = 16f;
		private Func<object[], bool> validateKey;
		private Action onAddButtonClicked;
		private bool drawInSingleRow;
		private Type keyType;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return keyType;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return false;
			}
		}

		private IDrawer KeyDrawer
		{
			get
			{
				return members[0];
			}
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="keyType"> They key type of the elements that can be added to the collection. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		public static CollectionAddFieldDrawer Create([NotNull]Type keyType, [CanBeNull]Func<object[],bool> validateKey, [CanBeNull]Action onAddButtonClicked, [CanBeNull]LinkedMemberInfo memberInfo, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool setReadOnly)
		{
			CollectionAddFieldDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CollectionAddFieldDrawer();
			}
			result.Setup(keyType, validateKey, onAddButtonClicked, memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the Create method");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the Create method");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setKeyType"> The type of the key used in the dictionary. Can not be null. </param>
		/// <param name="setValidateKey"> Function used for validating the current key value set up in the add button. Can be null. </param>
		/// <param name="setOnAddButtonClicked"> Delegate called when the add button is clicked. Can be null. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can not be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		private void Setup([NotNull]Type setKeyType, [CanBeNull]Func<object[], bool> setValidateKey, [CanBeNull]Action setOnAddButtonClicked, [CanBeNull]LinkedMemberInfo setMemberInfo, [NotNull]IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			keyType = setKeyType;
			drawInSingleRow = DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(keyType);
			validateKey = setValidateKey;
			onAddButtonClicked = setOnAddButtonClicked;
			base.Setup(keyType.DefaultValue(), keyType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			// No need to call OnValueChanged / OnMemberValueChanged in parent. We don't want members to get rebuilt for any reason.
			SetCachedValueSilent(memberValue);
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			Array.Resize(ref members, 2);

			var keyToAdd = DrawerProvider.GetForField(Value, keyType, null, this, GUIContent.none, ReadOnly);
			keyToAdd.OverrideValidateValue = validateKey;
			keyToAdd.OnKeyboardInputBeingGiven = OnKeyDrawerKeyboardInputBeingGiven;
			members[0] = keyToAdd;
			var button = ButtonDrawer.Create("", onAddButtonClicked, this, InspectorPreferences.Styles.AddButton, ReadOnly);
			members[1] = button;
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			// Pass some keyboard inputs over to first member.
			// E.g. pressing F2 should immediately start editing key field.
			// However, pass over navigation related events nor shortcuts.
			if(inputEvent.modifiers == EventModifiers.None)
			{
				var keyMember = MembersBuilt[0];
				if(keyMember.ShouldShowInInspector)
				{
					bool passOnInput = false;
				
					if(inputEvent.character != 0 && inputEvent.modifiers == EventModifiers.None)
					{
						passOnInput = true;
					}
					else
					{
						switch(inputEvent.keyCode)
						{
							case KeyCode.F2:
							case KeyCode.Return:
							case KeyCode.KeypadEnter:
								passOnInput = true;
								break;
						}
					}

					if(passOnInput)
					{
						keyMember.Select(ReasonSelectionChanged.KeyPressOther);
						keyMember.OnKeyboardInputGiven(inputEvent, keys);
						return true;
					}
				}
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <summary>
		/// Called when the key drawer is about to receive keyboard input.
		/// </summary>
		/// <param name="keyboardInputReceiver"> The text that will receive the keyboard input. </param>
		/// <param name="inputEvent"> Information about the keyboard input event including the key code. </param>
		/// <param name="keys"> The current key configuration. </param> <returns>
		/// True input was consumed by a listener and as such keyboardInputReceiver should never receive the keyboard input.
		/// </returns>
		private bool OnKeyDrawerKeyboardInputBeingGiven([NotNull]IDrawer keyboardInputReceiver, [NotNull]Event inputEvent, [NotNull]KeyConfigs keys)
		{
			if(keyboardInputReceiver is ITextFieldDrawer)
			{
				if(inputEvent.keyCode == KeyCode.Return && DrawGUI.EditingTextField)
				{
					if(onAddButtonClicked != null)
					{
						DrawGUI.EditingTextField = false;
						onAddButtonClicked();
						return true;
					}
				}
			}
			return false;
		}

		/// <inheritdoc/>
		public override bool DrawBodySingleRow(Rect position)
		{
			var rect1 = position;
			rect1.width -= addButtonWidth + buttonOffset;
			
			var rect2 = rect1;
			rect2.x += rect1.width + buttonOffset;
			rect2.width = addButtonWidth;

			return ParentDrawerUtility.DrawBodySingleRow(this, rect1, rect2);
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnCachedValueChanged(", applyToField, ", ", updateMembers, "): ", Value));
			#endif

			base.OnCachedValueChanged(applyToField, updateMembers);

			if(!ValueEquals(KeyDrawer.GetValue()))
			{
				#if DEV_MODE
				Debug.Assert(!KeyDrawer.Selected);
				#endif

				KeyDrawer.SetValue(Value, false, true);
			}
		}

		/// <inheritdoc cref="IDrawer.SetValue(object)" />
		public override bool SetValue(object setValue)
		{
			// If a value is e.g. pasted to the Add field, pass it on to the member.
			// No need to call OnValueChanged callbacks.
			SetCachedValueSilent(setValue);
			return KeyDrawer.SetValue(setValue, false, true);
		}
	}
}