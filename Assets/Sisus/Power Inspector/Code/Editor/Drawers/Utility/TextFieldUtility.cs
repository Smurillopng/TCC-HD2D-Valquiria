//#define DEBUG_EDITING_TEXT_FIELD
#define DEBUG_SYNC_EDITING_TEXT_FIELD

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

using UnityEngine;
using static Sisus.PI.NullExtensions;

namespace Sisus
{
	public static class TextFieldUtility
	{
		/// <summary> Call this when a text field control is clicked. </summary>
		/// <param name="subject"> The clicked text field drawer. </param>
		/// <param name="inputEvent"> The current input event. </param>
		public static void OnControlClicked(ITextFieldDrawer subject, Event inputEvent)
		{
			bool canStartEditing = CanStartEditing(subject);

			// if field was already selected when it was clicked, don't use the event
			// this way Unity can handle positioning the cursor in a specific point on the text field etc.
			if(subject.Selected)
			{
				#if DEV_MODE && DEBUG_EDITING_TEXT_FIELD
				Debug.Log(StringUtils.ToColorizedString("TextFieldUtility.OnControlClicked - DrawGUI.EditingTextField = ", canStartEditing, " (because was selected)"));
				#endif

				DrawGUI.EditingTextField = canStartEditing; 
				return;
			}

			DrawGUI.Use(inputEvent);
			subject.HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
			if(canStartEditing)
			{
				#if DEV_MODE && DEBUG_EDITING_TEXT_FIELD
				Debug.Log(StringUtils.ToColorizedString("TextFieldUtility.OnControlClicked - Calling StartEditingField (not selected and can start editing)"));
				#endif

				subject.StartEditingField();
			}
			else
			{
				#if DEV_MODE && DEBUG_EDITING_TEXT_FIELD
				Debug.Log(StringUtils.ToColorizedString("TextFieldUtility.OnControlClicked - DrawGUI.EditingTextField = ", false, " (because was can start editing was ", false, ")"));
				#endif

				DrawGUI.EditingTextField = false;
			}
		}

		/// <summary>
		/// Determines whether it is possible to start editing the text field drawer at this time,
		/// given the current state of the drawer and the inspector view.
		/// 
		/// Returns false if subject is read only or there's a multi-selection (editing multiple text fields
		/// simultaneously is not supported).
		/// </summary>
		/// <param name="subject"> The subject. </param>
		/// <returns> True if we can start editing, false if not. </returns>
		public static bool CanStartEditing(ITextFieldDrawer subject)
		{
			return !subject.ReadOnly && !InspectorUtility.ActiveManager.HasMultiSelectedControls;
		}

		#if UNITY_EDITOR
		/// <summary>
		/// If DrawGUI.EditingTextField and EditorGUIUtility.editingTextField values are not in sync,
		/// then figures out which should take precedence over the other, and syncs their values accordingly.
		/// </summary>
		public static void SyncEditingTextField()
		{
			if(EditorGUIUtility.editingTextField == DrawGUI.EditingTextField && !EditorGUIUtility.editingTextField)
			{
				return;
			}

			var manager = InspectorUtility.ActiveManager;
			if(manager == null || manager.SelectedInspector == null || manager.FocusedDrawer is null  || manager.SelectedInspector.InspectorDrawer == Null || !manager.SelectedInspector.InspectorDrawer.HasFocus)
			{
				DrawGUI.EditingTextField = EditorGUIUtility.editingTextField;
				return;
			}

			if(DrawGUI.EditingTextField == EditorGUIUtility.editingTextField)
			{
				if(EditorGUIUtility.editingTextField || GUIUtility.keyboardControl == 0)
				{
					return;
				}

				// GUI.TextArea and GUI.TextField don't use EditorGUIUtility.editingTextField, but GUIUtility.GetStateObject.
				try
				{
					if(GUIUtility.QueryStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is TextEditor)
					{
						DrawGUI.EditingTextField = true;
					}
				}
				// GUIUtility.QueryStateObject can throw a KeyNotFoundException.
				catch(Exception)
                {
					return;
                }
				return;
			}

			var lastInputEvent = DrawGUI.LastInputEvent();
			if(lastInputEvent == null)
			{
				DrawGUI.EditingTextField = EditorGUIUtility.editingTextField;
				return;
			}

			if(lastInputEvent.isMouse)
			{
				// Something other than Power Inspector could have been clicked.
				if(manager.MouseoveredInspector == null && manager.FocusedDrawer == Null)
				{
					#if DEV_MODE && DEBUG_SYNC_EDITING_TEXT_FIELD
					if(DrawGUI.EditingTextField != EditorGUIUtility.editingTextField) { Debug.Log("DrawGUI.editingTextField = "+StringUtils.ToColorizedString(EditorGUIUtility.editingTextField)+" with lastInputEvent="+StringUtils.ToString(lastInputEvent)+" because MouseoveredInspector="+StringUtils.Null); }
					#endif
					DrawGUI.EditingTextField = EditorGUIUtility.editingTextField;
				}
				// editing text fields is not allowed when multiple controls are selected
				else if(manager.HasMultiSelectedControls)
				{
					#if DEV_MODE && DEBUG_SYNC_EDITING_TEXT_FIELD
					if(DrawGUI.EditingTextField != false || EditorGUIUtility.editingTextField != false) { Debug.Log("DrawGUI.EditingTextField = "+StringUtils.False+" with lastInputEvent="+StringUtils.ToString(lastInputEvent)+" because HasMultiSelectedControls="+StringUtils.True); }
					#endif
					DrawGUI.EditingTextField = false;
				}
				else
				{
					#if DEV_MODE && DEBUG_SYNC_EDITING_TEXT_FIELD
					if(DrawGUI.EditingTextField != EditorGUIUtility.editingTextField) { Debug.LogWarning("EditorGUIUtility.editingTextField = "+StringUtils.ToColorizedString(DrawGUI.EditingTextField)+" with lastInputEvent="+StringUtils.ToString(lastInputEvent)); }
					#endif
					EditorGUIUtility.editingTextField = DrawGUI.EditingTextField;
				}
			}
			else
			{
				#if DEV_MODE && DEBUG_SYNC_EDITING_TEXT_FIELD
				if(DrawGUI.EditingTextField != EditorGUIUtility.editingTextField) { Debug.LogWarning("EditorGUIUtility.editingTextField = "+StringUtils.ToColorizedString(DrawGUI.EditingTextField)+" with lastInputEvent="+StringUtils.ToString(lastInputEvent)); }
				#endif
				EditorGUIUtility.editingTextField = DrawGUI.EditingTextField;
			}
		}
		#endif
	}
}