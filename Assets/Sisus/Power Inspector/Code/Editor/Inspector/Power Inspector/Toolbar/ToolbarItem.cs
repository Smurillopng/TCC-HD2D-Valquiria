//#define DEBUG_SELECTED
//#define DEBUG_DESELECTED
//#define DEBUG_KEYBOARD_INPUT

#if !POWER_INSPECTOR_LITE
using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	public abstract class ToolbarItem : IInspectorToolbarItem
	{
		[NotNull]
		protected IInspector inspector;
		
		[NotNull]
		protected IInspectorToolbar toolbar;

		protected Rect bounds;

		/// <inheritdoc/>
		public virtual ToolbarItemAlignment Alignment
		{
			get;
			private set;
		}

		/// <inheritdoc/>
		public virtual bool IsSearchBox
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public virtual bool Clickable
		{
			get
			{
				return Selectable;
			}
		}

		/// <inheritdoc/>
		public virtual bool Selectable
		{
			get
			{
				return ShouldShow();
			}
		}

		/// <inheritdoc/>
		public virtual int IndexInToolbar
		{
			get
			{
				return -1;
			}
		}

		/// <inheritdoc/>
		public abstract float MinWidth { get; }

		/// <inheritdoc/>
		public virtual float MaxWidth
		{
			get
			{
				return MinWidth;
			}
		}

		/// <inheritdoc/>
		public Rect Bounds
		{
			get
			{
				return bounds;
			}

			set
			{
				bounds = value;
				UpdateDrawPositions(value);
			}
		}
		

		/// <summary> Gets a value indicating whether this toolbar item has keyboard focus. </summary>
		/// <value> True if this is selected, false if not. </value>
		protected bool IsSelected
		{
			get
			{
				return toolbar.SelectedItem == this;
			}
		}

		/// <inheritdoc/>
		public virtual string DocumentationPageUrl
		{
			get
			{
				return "";
			}
		}

		protected bool HasDocumentationPage
		{
			get
			{
				return DocumentationPageUrl.Length > 0;
			}
		}

		/// <inheritdoc/>
		public Action<IInspectorToolbarItem, Rect, ActivationMethod> OnBeingActivated { get; set; }

		/// <inheritdoc/>
		public void Setup(IInspector inspectorContainingToolbar, IInspectorToolbar toolbarContainingItem, ToolbarItemAlignment alignment)
		{
			inspector = inspectorContainingToolbar;
			toolbar = toolbarContainingItem;
			bounds.width = 0f;
			Alignment = alignment;

			Setup();
		}

		/// <summary> Setup the inspector toolbar item so that it is ready to be drawn. </summary>
		protected virtual void Setup() { }

		/// <inheritdoc/>
		public virtual void OnSelected(ReasonSelectionChanged reason)
		{
			#if DEV_MODE && DEBUG_SELECTED
			Debug.Log(StringUtils.ToColorizedString(ToString()+"OnSelected(", reason, ")"));
			#endif

			KeyboardControlUtility.SetKeyboardControl(0, 3);
		}

		/// <inheritdoc/>
		public virtual void OnDeselected(ReasonSelectionChanged reason)
		{
			#if DEV_MODE && DEBUG_DESELECTED
			Debug.Log(StringUtils.ToColorizedString(ToString()+"OnDeselected(", reason, ")"));
			#endif
		}

		/// <summary> Called whenever the bounds of the toolbar item have changed during the Layout event. </summary>
		/// <param name="itemPosition"> The item position. </param>
		protected virtual void UpdateDrawPositions(Rect itemPosition) { }

		/// <inheritdoc/>
		public virtual void DrawBackground(Rect itemPosition, GUIStyle toolbarBackgroundStyle)
		{
			GUI.Label(itemPosition, GUIContent.none, toolbarBackgroundStyle);
		}

		/// <inheritdoc/>
		public void Draw(Rect itemPosition, bool mouseovered)
		{
			if(Event.current.type == EventType.Repaint)
			{
				OnRepaint(itemPosition);
			}
			else if(Event.current.type == EventType.Layout)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(itemPosition == Bounds);
				#endif

				OnLayout(itemPosition);
			}
			OnGUI(itemPosition, mouseovered);
		}

		/// <summary>
		/// Event that is called during every repaint event.
		/// 
		/// You can use this to handle drawing the GUI of your toolbar item.
		/// 
		/// Note that because this only gets called during the Repaint event, it does not
		/// support reacting to any user inputs, even if you use things like GUI.Button.
		/// 
		/// User interactions in toolbar items are usually handled through separate methods
		/// such as OnActivated and OnKeyboardEvent. If however your toolbar item's GUI needs
		/// to be called during other events besides Repaint, then you can override the OnGUI
		/// method instead of this one, and draw your item's GUI there.
		/// </summary>
		/// <param name="itemPosition"> The position and size at which to draw the item. </param>
		protected virtual void OnRepaint(Rect itemPosition) { }

		/// <summary> Called during every layout event. </summary>
		/// <param name="itemPosition"> The item position. </param>
		protected virtual void OnLayout(Rect itemPosition) { }

		/// <summary>
		/// Called during every single OnGUI event (Layout, Repaint, MouseDown...).
		/// 
		/// You can use this to draw reactive GUI elements that rely on events besides Repaint, such as GUI.Button and GUI.TextField.
		/// 
		/// If your GUI elements don't react to user input, or you can offload the interactivity to the other methods like
		/// OnClick and OnKeyboardEvent, you can use OnRepaint instead for drawing your item.
		/// </summary>
		/// <param name="itemPosition"> The item position. </param>
		/// <param name="itemPosition"> Is this item currently being mouseovered? </param>
		protected virtual void OnGUI(Rect itemPosition, bool mouseovered) { }

		/// <inheritdoc/>
		public virtual void DrawSelectionRect(Rect itemPosition)
		{
			DrawSelectionRect(itemPosition, GetSelectionRectColor());
		}

		protected Color GetSelectionRectColor()
		{
			#if UNITY_EDITOR
			if(inspector != null && inspector.InspectorDrawer as UnityEditor.EditorWindow != UnityEditor.EditorWindow.focusedWindow)
			{
				return inspector.Preferences.theme.ToolbarItemSelectedUnfocused;
			}
			#endif

			return inspector.Preferences.theme.ToolbarItemSelected;
		}

		protected virtual void DrawSelectionRect(Rect itemPosition, Color color)
		{
			var pos = itemPosition;
			pos.y += itemPosition.height - 2f;
			pos.height = 2f;

			#if UNITY_2019_3_OR_NEWER
			pos.width -= 1f;
			#else
			pos.width += 1f;
			#endif

			DrawGUI.Active.ColorRect(pos, color);
		}

		/// <inheritdoc/>
		public virtual bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inspector.IgnoreToolbarMouseInputs());
			Debug.Assert(inputEvent.type != EventType.Used);
			#endif

			if(HandleOnBeingActivated(inputEvent, ActivationMethod.LeftClick))
			{
				return true;
			}

			if(OnActivated(inputEvent, true))
			{
				DrawGUI.Use(inputEvent);
				GUIUtility.ExitGUI();
				return true;
			}
			return false;
		}

		/// <summary>
		/// This is be called when the toolbar item is being activated.
		/// It handles invoking the OnBeingActivated callback.
		/// </summary>
		/// <param name="inputEvent"> The input event. Can be null. </param>
		/// <param name="activationMethod"> The method of activation. </param>
		/// <returns> True if OnBeingActivated had listeners and one of them used the input event. If no input event was provided, always returns false. </returns>
		protected bool HandleOnBeingActivated([CanBeNull]Event inputEvent, ActivationMethod activationMethod)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert((activationMethod != ActivationMethod.LeftClick && activationMethod != ActivationMethod.RightClick && activationMethod != ActivationMethod.MiddleClick) || !inspector.IgnoreToolbarMouseInputs(), GetType().Name+ ".HandleOnBeingActivated called with IgnoreToolbarMouseInputs "+StringUtils.True);
			Debug.Assert(inputEvent.type != EventType.Used);
			#endif

			if(OnBeingActivated != null)
			{
				OnBeingActivated(this, Bounds, activationMethod);
				if(inputEvent != null && inputEvent.type == EventType.Used)
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc/>
		public virtual bool OnRightClick(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inspector.IgnoreToolbarMouseInputs(), GetType().Name+ ".HandleOnBeingActivated called with IgnoreToolbarMouseInputs "+StringUtils.True);
			Debug.Assert(inputEvent != null);
			Debug.Assert(inputEvent.type != EventType.Used);
			#endif

			bool extendedMenu = inputEvent.control || inspector.State.DebugMode;

			if(HandleOnBeingActivated(inputEvent, extendedMenu ? ActivationMethod.ExpandedContextMenu : ActivationMethod.RightClick))
			{
				return true;
			}

			return OpenContextMenu(inputEvent, extendedMenu);
		}

		protected bool OpenContextMenu([NotNull]Event inputEvent, bool extendedMenu)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inputEvent != null);
			Debug.Assert(!inputEvent.isMouse || !inspector.IgnoreToolbarMouseInputs());
			#endif

			var menu = Menu.Create();
			BuildContextMenu(ref menu, extendedMenu);

			#if DEV_MODE && UNITY_EDITOR
			if(extendedMenu)
			{
				AddDevModeDebuggingEntriesToRightClickMenu(ref menu);
			}
			#endif

			if(menu.Count == 0)
			{
				menu.Dispose();
				return false;
			}

			if(inputEvent.type != EventType.Used)
			{
				DrawGUI.Use(inputEvent);
			}

			ContextMenuUtility.OpenAt(menu, Bounds, true, inspector, InspectorPart.Toolbar, null, this);
			return true;
		}

		/// <summary>
		/// Builds the context menu that should be shown when the item is right clicked.
		/// </summary>
		/// <param name="menu"> Menu into which the built items should be added. </param>
		/// <param name="extendedMenu">
		/// Add advanced items to the menu? Some more rarely used items are only added to the context menu in extended mode, to reduce clutter.
		/// </param>
		protected virtual void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(HasDocumentationPage)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Help", OpenDocumentationPage);
			}
		}
		protected void OpenDocumentationPage()
		{
			var url = DocumentationPageUrl;
			PowerInspectorDocumentation.OpenUrl(url);
		}

		#if DEV_MODE && UNITY_EDITOR
		/// <summary>
		/// Adds debugging entries mean for developers only to opening right click menu.
		/// Invoked when control is right clicked with DEV_MODE preprocessor directive
		/// and the control key is held down.
		/// </summary>
		/// <param name="menu"> [in,out] The opening menu into which to add the menu items. </param>
		protected virtual void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			#if DEV_MODE && UNITY_EDITOR
			string scriptFilename = FileUtility.FilenameFromType(GetType());
			menu.Add("Debugging/Edit "+scriptFilename+".cs", ()=>
			{
				var script = FileUtility.FindScriptFile(GetType());
				if(script != null)
				{
					UnityEditor.AssetDatabase.OpenAsset(script);
				}
				else
				{
					Debug.LogError("FileUtility.FindScriptFilepath could not find file "+scriptFilename+".cs");
				}
			});
			menu.Add("Debugging/Print Full State", PrintFullStateForDevs);
			#endif
		}
		#endif

		#if DEV_MODE
		private void PrintFullStateForDevs()
		{
			DebugUtility.PrintFullStateInfo(this);
		}
		#endif


		/// <inheritdoc/>
		public virtual bool OnMiddleClick(Event inputEvent)
		{
			if(HandleOnBeingActivated(inputEvent, ActivationMethod.MiddleClick))
			{
				return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public virtual bool OnKeyboardInputGivenWhenNotSelected([NotNull]Event inputEvent, [NotNull]KeyConfigs keys)
		{
			return false;
		}

		/// <inheritdoc/>
		public virtual bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(ToString()+".OnKeyboardInputGiven("+StringUtils.ToString(inputEvent)+ ")");
			#endif

			if(keys.activate.DetectInput(inputEvent))
			{
				#if DEV_MODE
				Debug.Log(ToString()+" - activate input given!");
				#endif

				if(HandleOnBeingActivated(inputEvent, ActivationMethod.KeyboardActivate))
				{
					return true;
				}

				if(inputEvent.type != EventType.Used && OnActivated(inputEvent, false))
				{
					DrawGUI.Use(inputEvent);
					GUIUtility.ExitGUI();
					return true;
				}
			}

			if(inputEvent.keyCode == KeyCode.Menu)
			{
				if(OnMenuInputGiven(inputEvent))
				{
					DrawGUI.Use(inputEvent);
					GUIUtility.ExitGUI();
					return true;
				}
			}
			
			return false;
		}

		/// <summary> Called when the user has pressed the menu key on the keyboard. </summary>
		/// <param name="inputEvent"> The keyboard input event. </param>
		/// <returns> True if item consumed the input event, false if not. </returns>
		protected bool OnMenuInputGiven(Event inputEvent)
		{
			if(HandleOnBeingActivated(inputEvent, ActivationMethod.KeyboardMenu))
			{
				return true;
			}

			return OnRightClick(inputEvent);
		}

		/// <summary>
		/// Called during the validate command event.
		/// </summary>
		/// <param name="e"> The validate command event. </param>
		public virtual void OnValidateCommand(Event e)
		{
			switch(e.commandName)
			{
				case "Cut":
					OnCutCommandGiven();
					return;
				case "Copy":
					OnCopyCommandGiven();
					return;
				case "Paste":
					OnPasteCommandGiven();
					return;
				case "Find":
					OnFindCommandGiven();
					return;
			}
		}
		
		/// <summary>
		/// Called when the item is clicked or activated via keyboard activate input.
		/// </summary>
		/// <param name="inputEvent"> The input event. </param>
		/// <param name="isClick"> True if is a click event, false if not. </param>
		/// <returns> True if input event was consumed by the item, false if not. </returns>
		protected abstract bool OnActivated(Event inputEvent, bool isClick);

		/// <summary>
		/// Called when user has given the cut command with the toolbar item selected.
		/// </summary>
		protected virtual void OnCutCommandGiven() { }

		/// <summary>
		/// Called when user has given the copy command with the toolbar item selected.
		/// </summary>
		protected virtual void OnCopyCommandGiven() { }

		/// <summary>
		/// Called when user has given the paste command with the toolbar item selected.
		/// </summary>
		protected virtual void OnPasteCommandGiven() { }

		/// <inheritdoc/>
		public virtual bool OnFindCommandGiven()
		{
			return false;
		}

		/// <inheritdoc/>
		public virtual bool ShouldShow()
		{
			return true;
		}

		/// <inheritdoc/>
		public virtual void OnBecameInvisible() { }

		/// <inheritdoc/>
		public virtual void Dispose() { }
	}
}
#endif