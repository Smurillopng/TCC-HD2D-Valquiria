using JetBrains.Annotations;
using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer for a labeled Button, which invokes given Action when clicked.
	/// </summary>
	[Serializable]
	public sealed class ButtonDrawer : PrefixControlComboDrawer<string>
	{
		private const float VerticalPadding = 2f;

		/// <summary>
		/// Action to invoke when button is clicked.
		/// </summary>
		private Action onClicked;

		/// <summary>
		/// The label to display on the button
		/// </summary>
		private GUIContent buttonLabel;

		/// <summary> The style for the button. </summary>
		private GUIStyle style;

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

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(string);
			}
		}

		/// <inheritdoc/>
		public override float Height
		{
			get
			{
				return VerticalPadding + DrawGUI.SingleLineHeight + VerticalPadding;
			}
		}


		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <summary>
		/// Applies the value to field.
		/// </summary>
		protected override void ApplyValueToField() { }

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void DoReset() { CantSetValueError(); }

		/// <summary>
		/// Cant set value error.
		/// </summary>
		private void CantSetValueError(){ Debug.LogError("Button value can't be changed"); }

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="buttonText"> The text to shown on the button. </param>
		/// <param name="onClicked"> Action to invoke every time the button is clicked. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="readOnly"> True if button should be greyed out and not be interactive. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static ButtonDrawer Create(string buttonText, Action onClicked, IParentDrawer parent, GUIStyle style = null, bool readOnly = false)
		{
			ButtonDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ButtonDrawer();
			}
			result.Setup(GUIContentPool.Create(buttonText), onClicked, null, parent, null, readOnly, style);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="buttonText"> The text to shown on the button. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="readOnly"> True if button should be greyed out and not be interactive. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static ButtonDrawer Create(string buttonText, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIStyle style = null, bool readOnly = false)
		{
			ButtonDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ButtonDrawer();
			}
			result.Setup(GUIContentPool.Create(buttonText), null, memberInfo, parent, null, readOnly, style);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// </summary>
		/// <param name="setButtonLabel"> Sets the text label shown on the button. </param>
		/// <param name="doOnClicked"> Action that should get invoked every time the button is clicked. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="setParent"> Drawer whose member these Drawer. </param>
		/// <param name="setLabel"> The label (name) of the field. </param>
		/// <param name="setReadOnly"> True if button should be greyed out and not be interactive. </param>
		/// <param name="setStyle"> Style for button. If null, default button style will be used. </param>
		private void Setup([CanBeNull]GUIContent setButtonLabel, [CanBeNull]Action doOnClicked, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly, [CanBeNull]GUIStyle setStyle)
		{
			if(buttonLabel == null)
			{
				buttonLabel = GUIContent.none;
			}
			onClicked = doOnClicked;
			buttonLabel = setButtonLabel;
			style = setStyle == null ? InspectorPreferences.Styles.Button : setStyle;
			Setup(setButtonLabel.text, typeof(string), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public override string DrawControlVisuals(Rect position, string buttonText)
		{
			var drawRect = position;
			drawRect.y += VerticalPadding;
			const float reduceHeight = VerticalPadding + VerticalPadding;
			drawRect.height -= reduceHeight;

			if(DrawGUI.Active.Button(drawRect, buttonLabel, style))
			{
				OnClick();
			}
			return Value;
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			#if !POWER_INSPECTOR_LITE
			menu.Add("Copy", CopyToClipboard);
			#endif
			AddMenuItemsFromAttributes(ref menu);
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(DrawGUI.EditingTextField)
			{
				return false;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					OnClick();
					return true;
			}
			return false;
		}

		/// <inheritdoc/>
		protected override string GetRandomValue()
		{
			throw new NotSupportedException("Randomize not supported.");
		}

		/// <summary>
		/// Called when the button is clicked
		/// </summary>
		private void OnClick()
		{
			if(onClicked != null)
			{
				onClicked();
			}
			else
			{
				Debug.LogError("Null onClick on button!");
			}
		}

		/// <summary>
		/// Gets has unapplied changes updated.
		/// </summary>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}
	}
}