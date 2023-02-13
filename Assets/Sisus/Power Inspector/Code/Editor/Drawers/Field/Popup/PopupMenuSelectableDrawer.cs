#define ENABLE_FILTERABLE_ENUM_GUI_DRAWER

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	public abstract class PopupMenuSelectableDrawer<TValue> : PrefixControlComboDrawer<TValue>
	{
		protected static List<PopupMenuItem> generatedMenuItems = new List<PopupMenuItem>(30);
		protected static Dictionary<string, PopupMenuItem> generatedGroupsByLabel = new Dictionary<string, PopupMenuItem>(10);
		protected static Dictionary<string, PopupMenuItem> generatedItemsByLabel = new Dictionary<string, PopupMenuItem>(20);

		private static bool menuItemsGenerated;
		private static readonly List<PopupMenuItem> tickedMenuItems = new List<PopupMenuItem>(1);
		private static Type menuItemsGeneratedForContext;

		protected Type typeContext;

		private bool typeContextDetermined;
		private readonly GUIContent selectedItemLabel = new GUIContent("");
		private bool mouseIsOverButton;

		private Rect buttonRect;

		/// <summary> Gets a value indicating whether multiple items can be ticked in the menu. </summary>
		/// <value> True if we can tick multiple items in the menu, false if not. </value>
		protected virtual bool CanTickMultipleItems
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		public override TValue DrawControlVisuals(Rect position, TValue inputValue)
		{
			DrawGUI.Active.Label(position, selectedItemLabel, EditorStyles.popup);
			return Value;
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			if(mouseIsOverButton)
			{
				// From version 2019.3 onwards Unity has built-in mouseover effects for enum fields
				#if !UNITY_2019_3_OR_NEWER
				DrawGUI.DrawMouseoverEffect(buttonRect, localDrawAreaOffset);
				#endif
				return;
			}

			base.OnMouseover();
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(keys.activate.DetectAndUseInput(inputEvent))
			{
				OpenMenu();
				return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			UpdateSelectedItemLabel(Value);
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		/// <summary>
		/// Get menu label text for value.
		/// </summary>
		/// <param name="value"> Possible value for drawer shown in the menu. </param>
		/// <returns></returns>
		protected virtual string GetLabelText([NotNull]TValue value)
		{
			return value.ToString();
		}

		protected virtual string GetTooltip([NotNull]TValue value)
		{
			return "";
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((TValue)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(TValue setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			UpdateSelectedItemLabel(setValue);
		}

		/// <summary> Generate menu items for popup menu. </summary>
		/// <param name="rootItems"> [in,out] The root items of the menu. </param>
		/// <param name="groupsByLabel"> [in,out] All groups in the menu flattened with full menu path as key in dictionary. </param>
		/// <param name="itemsByLabel"> [in,out] All non-group leaf items in the menu flattened with full menu path as key in dictionary. </param>
		protected abstract void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel);

		protected virtual void GetTickedMenuItems(List<PopupMenuItem> rootItems, List<PopupMenuItem> results)
		{
			var find = PopupMenuUtility.FindMenuItemByIdentifyingObject(rootItems, Value);
			if(find != null)
			{
				results.Add(find);
			}
		}

		/// <summary> Gets a Type the Assembly of which defines which types are visible in the current context. </summary>
		/// <returns> The type context. </returns>
		protected virtual Type GetTypeContext()
		{
			return Type;
		}

		private void UpdateSelectedItemLabel(TValue value)
		{
			if(value == null)
			{
				selectedItemLabel.text = "";
				selectedItemLabel.tooltip = "";
			}
			else
			{
				selectedItemLabel.text = GetLabelText(value);
				selectedItemLabel.tooltip = GetTooltip(value);
			}
		}
		
		/// <inheritdoc />
		protected override void OnControlClicked(Event inputEvent)
		{
			#if DEV_MODE
			Profiler.BeginSample("OnControlClicked.OpenMenu");
			#endif

			base.OnControlClicked(inputEvent);
			OpenMenu();

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);

			mouseIsOverButton = buttonRect.MouseIsOver();
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			buttonRect = ControlPosition;
			buttonRect.height -= 2f;
		}

		/// <summary>
		/// If some form of async setup is needed before menu can be opened
		/// this should return false until setup is finished.
		/// </summary>
		/// <returns> True if can open menu, false if not. </returns>
		protected virtual bool IsReadyToGenerateMenuItems()
		{
			return true;
		}

		protected bool GenerateMenuItemsIfNotGenerated()
		{
			if(!IsReadyToGenerateMenuItems())
			{
				return false;
			}

			if(!typeContextDetermined)
			{
				typeContextDetermined = true;
				typeContext = GetTypeContext();
			}

			if(!menuItemsGenerated || menuItemsGeneratedForContext != typeContext)
			{
				#if DEV_MODE
				Debug.Log(Msg("Generating menu items for type context "+ typeContext.Name));
				#endif

				menuItemsGenerated = true;
				
				generatedMenuItems.Clear();
				generatedGroupsByLabel.Clear();
				generatedItemsByLabel.Clear();
				GenerateMenuItems(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel);
				menuItemsGeneratedForContext = typeContext;
			}

			return true;
		}

		private void OpenMenu()
		{
			#if DEV_MODE
			Profiler.BeginSample("PopupMenuSelectableDrawer.OpenMenu");
			#endif

			if(!GenerateMenuItemsIfNotGenerated())
			{
				Debug.LogWarning("Can open menu yet. Setup still in progress...");
				return;
			}

			tickedMenuItems.Clear();
			GetTickedMenuItems(generatedMenuItems, tickedMenuItems);
			
			var inspector = InspectorUtility.ActiveInspector;

			var openAt = ControlPosition;

			var offset = GUISpace.ConvertPoint(localDrawAreaOffset, Space.Screen, Space.Local);
			openAt.x += offset.x;
			openAt.y += offset.y;

			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString("openAt=", openAt, ", ControlPosition=", ControlPosition, ", offset=", offset, ", localDrawAreaOffset=", localDrawAreaOffset));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!(this is FieldInfoDrawer) || generatedMenuItems == MemberInfoDrawerUtility.fieldRootItems);
			Debug.Assert(!(this is FieldInfoDrawer) || generatedMenuItems != MemberInfoDrawerUtility.methodRootItems);
			Debug.Assert(generatedMenuItems != MemberInfoDrawerUtility.fieldRootItems || !(generatedMenuItems[1].GetFirstNonGroupChild() is PopupMenuItem first) || first.IdentifyingObject.GetType() == typeof(System.Reflection.FieldInfo));
			#endif

			PopupMenuManager.Open(inspector, generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, tickedMenuItems, CanTickMultipleItems, openAt, OnPopupMenuItemClicked, OnPopupMenuClosed, MenuLabel(), this);

			if(inspector.Preferences.popupMenusScrollToActiveItem)
			{
				var select = Value;
				if(select != null)
				{
					PopupMenuManager.SelectItem(GetPopupItemLabel(select));
				}
			}

			#if DEV_MODE
			Profiler.EndSample();
			#endif
		}

		/// <summary> Get label to display at the top of the popup menu. </summary>
		/// <returns> A label. </returns>
		protected abstract GUIContent MenuLabel();
		
		protected virtual string GetPopupItemLabel(TValue value)
		{
			return value.ToString();
		}

		/// <summary> Called when an item in the popup menu is clicked. </summary>
		/// <param name="item"> Information about clicked item. </param>
		protected virtual void OnPopupMenuItemClicked(PopupMenuItem item)
		{
			try
			{
				Value = (TValue)item.IdentifyingObject;
			}
			catch(Exception e)
			{
				Debug.LogError("Failed to cast item IdentifyingObject from type "+StringUtils.TypeToString(item.IdentifyingObject)+" to "+ StringUtils.ToString(typeof(TValue)) + ": " + e);
			}
		}

		private void OnPopupMenuClosed()
		{
			Select(ReasonSelectionChanged.Initialization);
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			typeContextDetermined = false;
			mouseIsOverButton = false;
			base.Dispose();
		}
	}
}