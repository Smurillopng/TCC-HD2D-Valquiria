#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEditor;

#if CSHARP_7_3_OR_NEWER
using Sisus.Vexe.FastReflection;
#endif

namespace Sisus
{
	/// <summary>
	/// Editor-only class that uses reflection to access internal data in some built-in classes.
	/// </summary>
	public class KeyboardControlInfo
	{
		private MethodInfo getKeyboardRect;
		private FieldInfo lastControlIdField;

		private object[] GetKeyboardRectParams = {0, default(Rect)};
		
		#if CSHARP_7_3_OR_NEWER
		private MethodCaller<object, object> canHaveKeyboardFocus;
		#else
		private MethodInfo canHaveKeyboardFocus;
		#endif

		public KeyboardControlInfo()
		{
			var editorGUIUtilityType = typeof(EditorGUIUtility);
			getKeyboardRect = editorGUIUtilityType.GetMethod("Internal_GetKeyboardRect", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static);
			lastControlIdField = typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

			#if CSHARP_7_3_OR_NEWER
			canHaveKeyboardFocus = editorGUIUtilityType.GetMethod("CanHaveKeyboardFocus", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static).DelegateForCall();
			#else
			canHaveKeyboardFocus = editorGUIUtilityType.GetMethod("CanHaveKeyboardFocus", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static);
			#endif
		}

		/// <summary>
		/// Gets Rect describing the position and size of the inspector control that currently has keyboard focus
		/// </summary>
		/// <value>
		/// Rect (position and size)
		/// </value>
		public Rect KeyboardRect
		{
			get
			{
				GetKeyboardRectParams[0] = GUIUtility.keyboardControl;
				
				getKeyboardRect.Invoke(null, GetKeyboardRectParams);
				//second parameter has the out modifier
				return (Rect)GetKeyboardRectParams[1];
			}
		}
		
		public int LastControlID
        {
			get
            {
				return (int)lastControlIdField.GetValue(null);
            }
        }

		public bool CanHaveKeyboardFocus(int id)
		{
			return (bool)canHaveKeyboardFocus.InvokeWithParameter(null, id);
		}
	}
}
#endif