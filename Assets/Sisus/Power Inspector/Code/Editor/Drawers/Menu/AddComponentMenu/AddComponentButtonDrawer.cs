//#define DEBUG_ON_CLICK
//#define DEBUG_OPEN

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Drawer representing the Add Component button that is drawn as the last member of GameObjectDrawer.
	/// </summary>
	[Serializable]
	public sealed class AddComponentButtonDrawer : BaseDrawer
	{
		public delegate void OpenAddComponentMenu(IInspector inspector, GameObjectDrawer target, Rect unrollPosition, Action onClosed);

		private const float ButtonWidth = 228f;
		private const float ButtonHeight = 23f;
		private const float TotalHeightClosed = 52f;
		
		private bool open; //used by Unfolded internally

		private Rect splitterRect;
		private Rect buttonRect;
		
		/// <summary>
		/// for AddComponentMenuWindow to hook into the opening
		/// </summary>
		public static OpenAddComponentMenu onOpen;

		private IInspector inspector;

		/// <inheritdoc />
		public override bool ReadOnly
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
				return ShownInInspector;
			}
		}

		/// <inheritdoc />
		protected override Rect SelectionRect
		{
			get
			{
				var rect = buttonRect;
				rect.width += 2f;
				return rect;
			}
		}

		/// <inheritdoc />
		public override Rect ClickToSelectArea
		{
			get
			{
				return buttonRect;
			}
		}

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				//so can copy paste the filter
				return typeof(string);
			}
		}

		private static string FilterString
		{
			get
			{
				return Platform.Active.GetPrefs("AddComponent.FilterString", "");
			}

			set
			{
				Platform.Active.SetPrefs("AddComponent.FilterString", value);
			}
		}
		
		/// <inheritdoc />
		public override float Height
		{
			get
			{
				return TotalHeightClosed;
			}
		}

		public static bool OpenSelectedOrFirstFoundInstance(IInspector selectedInspector)
		{
			var selectedControl = selectedInspector.FocusedDrawer;
			
			if(selectedControl != null)
			{
				for(var next = selectedControl; next != null; next = next.Parent)
				{
					var go = next as IGameObjectDrawer;
					if(go != null)
					{
						var addComponentButton = go.AddComponentButton;
						addComponentButton.Select(ReasonSelectionChanged.KeyPressShortcut);
						addComponentButton.Open();
						return true;
					}
				}
			}

			var inspected = selectedInspector.State.drawers.Members;
			for(int n = 0; n < inspected.Length; n++)
			{
				var go = inspected[n] as IGameObjectDrawer;
				if(go != null)
				{
					var addComponentButton = go.AddComponentButton;
					addComponentButton.Select(ReasonSelectionChanged.KeyPressShortcut);
					addComponentButton.Open();
					return true;
				}
			}
			return false;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> Inspector that contains the drawers. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static AddComponentButtonDrawer Create([NotNull]IGameObjectDrawer parent, [NotNull]IInspector inspector)
		{
			AddComponentButtonDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AddComponentButtonDrawer();
			}
			result.Setup(parent, null, inspector);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawers from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private AddComponentButtonDrawer() { }

		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of AddComponentButtonDrawer.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setInspector"> Inspector that contains the drawers. </param>
		private void Setup([NotNull]IGameObjectDrawer setParent, [CanBeNull]GUIContent setLabel, IInspector setInspector)
		{
			open = false;
			inspector = setInspector;
			base.Setup(setParent, setLabel != null ? setLabel : GUIContentPool.Create("Add Component", "Default Shortcut:\nCtrl + T"));
		}

		/// <inheritdoc />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			return DrawGUI.MinAutoSizedPrefixLabelWidth;
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			splitterRect = position;
			splitterRect.height = 1f;
			splitterRect.width = Screen.width;

			buttonRect = position;
			buttonRect.y += 15f;
			buttonRect.width = ButtonWidth;
			//rounding to a whole number is important to avoid blurry text and textures
			buttonRect.x = Mathf.FloorToInt(DrawGUI.InspectorWidth * 0.5f - ButtonWidth * 0.5f);
			buttonRect.height = ButtonHeight;

			lastDrawPosition = position;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(splitterRect.height < 1f)
			{
				GetDrawPositions(position);
				Inspector.RefreshView();
			}
			
			DrawGUI.DrawLine(splitterRect, inspector.Preferences.theme.ComponentSeparatorLine);

			GUI.Label(buttonRect, new GUIContent("Add Component", "Add a component to this GameObject (Ctrl + T)"), InspectorPreferences.Styles.AddComponentButton);

			DrawGUI.LayoutSpace(Height);

			return false;
		}

		/// <inheritdoc />
		public override void DrawSelectionRect()
		{
			var rect = buttonRect;
			rect.x += 1f;
			DrawGUI.DrawControlSelectionIndicator(rect, localDrawAreaOffset);
		}

		private void Open()
		{
			open = true;

			KeyboardControlUtility.KeyboardControl = 0;

			if(!inspector.InspectorDrawer.HasFocus)
			{
				inspector.InspectorDrawer.FocusWindow();
			}
			InspectorUtility.ActiveManager.ActiveInspector = inspector;
			
			#if DEV_MODE && DEBUG_OPEN
			Debug.Log(GetType().Name + " - Calling onOpen with parent "+parent+" and inspector "+ inspector +"...");
			#endif

			onOpen(inspector, parent as GameObjectDrawer, buttonRect, Close);
		}

		private void Close()
		{
			open = false;
		}

		/// <inheritdoc />
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_ON_CLICK
			Debug.Log(GetType().Name+ " - OnClick with mousePos="+inputEvent.mousePosition+" vs ClickToSelectArea="+ClickToSelectArea);
			#endif

			if(!Selected)
			{
				Select(ReasonSelectionChanged.ControlClicked);
			}

			if(!open)
			{
				Open();
				DrawGUI.Use(inputEvent);
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(InspectorUtility.ActiveInspector.Preferences.keyConfigs.addComponent.DetectAndUseInput(inputEvent))
			{
				Select(ReasonSelectionChanged.KeyPressShortcut);
				Open();
				return true;
			}

			switch(Event.current.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					if(!open)
					{
						Open();
					}
					return true;
				//TO DO: implement "opening" namespace or going back one level
				case KeyCode.LeftArrow:
				case KeyCode.RightArrow:
					DrawGUI.Use(inputEvent);
					return true;
				default:
					return base.OnKeyboardInputGiven(inputEvent, keys);
			}
		}

		/// <summary>
		/// Even though the AddComponentMenuButton isn't really a component
		/// it can still be intuitive to be able to use the select previous component
		/// shortcut keys to jump to the previous component
		/// </summary>
		public override void SelectPreviousComponent()
		{
			ComponentDrawerUtility.SelectPreviousVisibleComponent(this);
		}

		/// <summary>
		/// Even though the AddComponentMenuButton isn't really a component
		/// it can still be intuitive to be able to use the select next component
		/// shortcut keys to jump to the next component (in stacked edit mode)
		/// </summary>
		public override void SelectNextComponent()
		{
			ComponentDrawerUtility.SelectNextVisibleComponent(this);
		}

		/// <inheritdoc />
		public override bool SetValue(object newValue)
		{
			string setValue = newValue as string;
			if(!string.Equals(FilterString, setValue))
			{
				FilterString = setValue;
				return true;
			}
			return false;
		}

		/// <inheritdoc />
		public override object GetValue(int index)
		{
			return FilterString;
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			if(!open)
			{
				if(SelectionRect.Contains(Event.current.mousePosition))
				{
					DrawGUI.DrawMouseoverEffect(SelectionRect, localDrawAreaOffset);
				}
				
				DrawGUI.Active.AddCursorRect(SelectionRect, MouseCursor.Link);
			}
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			var addHistory = AddComponentHistoryTracker.LastAddedComponents;

			Type typeFromClipboard = null;
			if(Clipboard.CanPasteAs(Types.Type))
			{
				#if DEV_MODE
				Debug.Log("Clipboard.CanPasteAs(Type): "+StringUtils.True);
				#endif

				typeFromClipboard = Clipboard.Paste<Type>();

				#if DEV_MODE
				Debug.Log(Msg("typeFromClipboard: ", typeFromClipboard));
				#endif
			}
			
			if(typeFromClipboard == null)
			{
				#if DEV_MODE
				Debug.Log("Clipboard.CanPasteAs(Type): "+StringUtils.False);
				#endif
				var objectReference = Clipboard.ObjectReference;

				#if DEV_MODE
				Debug.Log(Msg("objectReference: ", objectReference));
				#endif

				if(objectReference != null)
				{
					#if UNITY_EDITOR
					var script = objectReference as UnityEditor.MonoScript;
					if(script != null)
					{
						typeFromClipboard = script.GetClass();
					}
					else
					#endif
					{
						typeFromClipboard = objectReference.GetType();
					}

					#if DEV_MODE
					Debug.Log(Msg("typeFromClipboard: ", typeFromClipboard));
					#endif
				}
			}

			var gameObjectDrawer = parent as GameObjectDrawer;

			if(typeFromClipboard != null && typeFromClipboard.IsComponent())
			{
				addHistory.Remove(typeFromClipboard);

				if(!AddComponentUtility.HasConflictingMembers(typeFromClipboard, gameObjectDrawer))
				{
					menu.Add(typeFromClipboard.Name, ()=>QuickAddComponent(typeFromClipboard));
				}
				else
				{
					menu.AddDisabled(typeFromClipboard.Name);
				}

				if(addHistory.Count > 0)
				{
					menu.AddSeparator();
				}
			}
			
			for(int n = 0, count = addHistory.Count; n < count; n++)
			{
				var type = addHistory[n];
				if(!AddComponentUtility.HasConflictingMembers(type, gameObjectDrawer))
				{
					menu.Add(type.Name, ()=>QuickAddComponent(type));
				}
				else
				{
					menu.AddDisabled(type.Name);
				}
			}
		}

		private void QuickAddComponent(Type type)
		{
			var gameObjectDrawer = (IGameObjectDrawer)parent;
			var adder = AddComponentMenuDrawer.CreateNewBackgroundInstance(inspector, gameObjectDrawer);
			adder.SetTarget(inspector, gameObjectDrawer);
			adder.AddComponent(type);
		}


		/// <inheritdoc />
		protected override void DoRandomize()
		{
			var componentTypes = TypeExtensions.ComponentTypes;
			var componentType = componentTypes[UnityEngine.Random.Range(0, componentTypes.Length)];
			((IGameObjectDrawer)parent).AddComponent(componentType, false);
		}

		public override void Dispose()
		{
			splitterRect.height = 0f;
			base.Dispose();
		}
	}
}