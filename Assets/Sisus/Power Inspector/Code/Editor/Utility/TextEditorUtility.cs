using System;
using System.Reflection;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Utility class for things related to the TextEditor currently active in the Editor.
	/// Utilizes Reflection to call internal methods in Unity's EditorGUI class.
	/// </summary>
	public static class TextEditorUtility
	{
		private static Type editorGUIType = typeof(UnityEditor.EditorGUI);
		private static FieldInfo recycledEditorField;
		private static FieldInfo activeEditorField;
		private static bool setupDone;

		private static void Setup()
		{
			recycledEditorField = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
			activeEditorField = editorGUIType.GetField("activeEditor", BindingFlags.NonPublic | BindingFlags.Static);
			setupDone = true;
		}

		public static int ActiveEditorCursorIndex()
        {
			var activeEditor = ActiveEditor();
			return activeEditor == null ? -1 : activeEditor.cursorIndex;
		}

		public static TextEditor ActiveEditor()
        {
			if(!setupDone)
            {
				Setup();
            }

			if(GUIUtility.keyboardControl == 0)
            {
				return null;
            }

			var editorEditor = activeEditorField.GetValue(null) as TextEditor;
			if(editorEditor != null)
            {
				return editorEditor;
            }

			// GUI.TextArea and GUI.TextField don't use EditorGUIUtility.editingTextField, but GUIUtility.GetStateObject.
			try
			{
				return GUIUtility.QueryStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
			}
			// GUIUtility.QueryStateObject can throw a KeyNotFoundException.
			catch
            {
				return null;
            }
		}

		public static TextEditor GetActiveOrRecycledTextEditor()
		{
			if(!setupDone)
			{
				Setup();
			}

			var activeEditor = ActiveEditor();
			if(activeEditor != null)
            {
				return activeEditor;
            }

			return recycledEditorField.GetValue(null) as TextEditor;
		}

		public static void MoveCursorToTextEditorEnd()
		{
			var textEditor = GetActiveOrRecycledTextEditor();
			if(textEditor != null)
			{
				textEditor.SelectNone();
				textEditor.MoveTextEnd();
				#if DEV_MODE
				Debug.Log("TextEditorUtility.MoveCursorToTextEditorEnd" + "\ntext ="+textEditor.text+ "\ncursorIndex="+ textEditor.cursorIndex+", selectIndex="+textEditor.selectIndex);
				#endif
			}
		}

		public static void SetText(string text)
		{
			var textEditor = GetActiveOrRecycledTextEditor();
			if(textEditor != null)
			{
				textEditor.text = text;
				#if DEV_MODE
				Debug.Log("TextEditorUtility.SetText("+ text + ")\ntextEditor.text=" + textEditor.text+ "\ncursorIndex="+ textEditor.cursorIndex+", selectIndex="+textEditor.selectIndex+", DrawGUI.EditingTextField="+ DrawGUI.EditingTextField);
				#endif
			}
		}

		public static void SelectAllText()
		{
			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			bool editingTextFieldWas = UnityEditor.EditorGUIUtility.editingTextField;
			#endif

			var textEditor = GetActiveOrRecycledTextEditor();
			if(textEditor != null)
			{
				textEditor.SelectAll();
				#if DEV_MODE
				Debug.Log("TextEditorUtility.SelectAllText" + "\ntext="+textEditor.text+ "\ncursorIndex="+ textEditor.cursorIndex+", selectIndex="+textEditor.selectIndex+", DrawGUI.EditingTextField="+ DrawGUI.EditingTextField);
				#endif
			}
			#if DEV_MODE
			else { Debug.Log("TextEditorUtility.SelectAllText - Can't select all text because current TextEditor was null."); }
			#endif

			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			Debug.Assert(editingTextFieldWas == UnityEditor.EditorGUIUtility.editingTextField);
			#endif
		}

		public static bool SetTextLength(int length)
		{
			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			bool editingTextFieldWas = UnityEditor.EditorGUIUtility.editingTextField;
			#endif

			var textEditor = GetActiveOrRecycledTextEditor();
			if(textEditor != null && textEditor.text.Length > length)
			{
				textEditor.text = textEditor.text.Substring(0, length);
				#if DEV_MODE
				Debug.Log("TextEditorUtility.SetTextLength(1)" + "\ntext =" + textEditor.text + "\ncursorIndex=" + textEditor.cursorIndex + ", selectIndex=" + textEditor.selectIndex);
				#endif

				#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
				Debug.Assert(editingTextFieldWas == UnityEditor.EditorGUIUtility.editingTextField);
				#endif

				return true;
			}
			#if DEV_MODE
			else if(textEditor == null) { Debug.Log("TextEditorUtility.SetTextLength - Can't set text length because TextEditor was null."); }
			#endif

			#if DEV_MODE && PI_ASSERTATIONS && UNITY_EDITOR
			Debug.Assert(editingTextFieldWas == UnityEditor.EditorGUIUtility.editingTextField);
			#endif

			return false;
		}

		public static bool Insert(char character)
		{
			var textEditor = GetActiveOrRecycledTextEditor();
			if(textEditor != null)
			{
				textEditor.Insert(character);
				#if DEV_MODE
				Debug.Log("TextEditorUtility.Insert("+StringUtils.ToString(character)+")" + "\ntext =" + textEditor.text + "\ncursorIndex=" + textEditor.cursorIndex + ", selectIndex=" + textEditor.selectIndex);
				#endif
				return true;
			}
			return false;
		}
	}
}