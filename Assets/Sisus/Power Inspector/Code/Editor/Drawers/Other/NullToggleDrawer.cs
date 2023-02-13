using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// A toggle field whose state is false when its parent drawers'
	/// value is null, and true otherwise.
	/// The look of the control can be customized with a GUIStyle.
	/// Functionally this works similarly to ButtonDrawer:
	/// an Action is invoked whenever the toggle field is clicked. 
	/// </summary>
	[Serializable]
	public sealed class NullToggleDrawer : BaseDrawer
	{
		private Action onClicked;
		private GUIStyle style;
		private bool readOnly;
		
		/// <inheritdoc />
		public override bool ReadOnly
		{
			get
			{
				return readOnly;
			}
		}

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return typeof(bool);
			}
		}

		/// <inheritdoc />
		public override bool Clickable
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		public override bool Selectable
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// The active state of the toggle control, as determined by whether or not the value of the parent drawers is null or not.
		/// </summary>
		/// <value> True if active, false if inactive. </value>
		private bool State
		{
			get
			{
				return parent.GetValue() != null;
			}
		}

		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			return parent.GetValue(index) != null;
		}

		/// <inheritdoc/>
		public override object GetValue()
		{
			return State;
		}

		/// <inheritdoc/>
		public override bool SetValue(object setValue, bool applyToField, bool updateMembers)
		{
			if(State != (bool)setValue)
			{
				OnClick(Event.current);
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="onClicked"> Action to invoke every time the button is clicked. </param>
		/// <param name="parent">
		/// The parent drawers of created drawers. This can NOT be null because the state of the toggle
		/// control is determined by whether or not the the value of the parent drawers is null or not.
		/// </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static NullToggleDrawer Create(Action onClicked, [NotNull]IParentDrawer parent, bool readOnly)
		{
			return Create(onClicked, InspectorPreferences.Styles.NullToggle, parent, readOnly);
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="onClicked"> Action to invoke every time the button is clicked. </param>
		/// <param name="guiStyle"> GUIStyle specifying how the toggle control should look. </param>
		/// <param name="parent">
		/// The parent drawers of created drawers. This can NOT be null because the state of the toggle
		/// control is determined by whether or not the the value of the parent drawers is null or not.
		/// </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static NullToggleDrawer Create(Action onClicked, GUIStyle guiStyle, [NotNull]IParentDrawer parent, bool readOnly)
		{
			NullToggleDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new NullToggleDrawer();
			}
			result.Setup(onClicked, guiStyle, parent, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawers from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private NullToggleDrawer() { }

		/// <inheritdoc/>
		protected override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of NullToggleDrawer.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setOnClicked"> Action to invoke every time the button is clicked. </param>
		/// <param name="setGUIStyle"> GUIStyle specifying how the toggle control should look. </param>
		/// <param name="setParent">
		/// The parent drawers of created drawers. This can NOT be null because the state of the toggle
		/// control is determined by whether or not the parent's value is null or not.
		/// </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		private void Setup([CanBeNull]Action setOnClicked, [CanBeNull]GUIStyle setGUIStyle, [NotNull]IParentDrawer setParent, bool setReadOnly)
		{
			readOnly = setReadOnly;
			style = setGUIStyle;
			onClicked = setOnClicked;
			base.Setup(setParent, GUIContent.none);
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			if(ReadOnly)
			{
				bool guiWasEnabled = GUI.enabled;
				GUI.enabled = false;
				GUI.Toggle(position, State, GUIContent.none, style);
				GUI.enabled = guiWasEnabled;
			}
			else
			{
				GUI.Toggle(position, State, GUIContent.none, style);
			}
			return false;
		}

		/// <inheritdoc/>
		public override void OnMouseover()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Clickable);
			#endif

			if(!ReadOnly)
			{
				DrawGUI.DrawMouseoverEffect(ClickToSelectArea, localDrawAreaOffset);
			}
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					OnClick(inputEvent);
					return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc />
		public override void DrawSelectionRect()
		{
			DrawGUI.DrawSelectionRect(SelectionRect, localDrawAreaOffset);
		}

		/// <inheritdoc />
		protected override void DoRandomize()
		{
			if(RandomUtils.Bool())
			{
				OnClick(Event.current);
			}
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			onClicked = null;
			style = null;
			base.Dispose();
		}

		/// <inheritdoc />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			#endif

			if(onClicked != null)
			{
				DrawGUI.Use(inputEvent);
				parent.Select(ReasonSelectionChanged.ControlClicked);
				onClicked();
			}
			else
			{
				Debug.LogError(ToString() + " - onClick Action was null!");
			}
			return true;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			menu.Add("Toggle", ()=>onClicked());
		}

		private static void CantSetValueError() { Debug.LogError("Button value can't be changed"); }
	}
}