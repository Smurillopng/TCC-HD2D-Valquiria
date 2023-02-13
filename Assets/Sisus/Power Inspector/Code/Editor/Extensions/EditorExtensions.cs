using JetBrains.Annotations;
using System.Reflection;
using UnityEditor;

namespace Sisus
{
    public static class EditorExtensions
    {
		private static readonly FieldInfo serializedObjectField;
		private static readonly MethodInfo canBeExpandedViaAFoldout;
        private static readonly PropertyInfo firstInspectedEditor;

        static EditorExtensions()
        {
			serializedObjectField = typeof(Editor).GetField("m_SerializedObject", BindingFlags.Instance | BindingFlags.NonPublic);
			canBeExpandedViaAFoldout = typeof(Editor).GetMethod("CanBeExpandedViaAFoldout", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            firstInspectedEditor = typeof(Editor).GetProperty("firstInspectedEditor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

		[CanBeNull]
        internal static SerializedObject GetSerializedObject([NotNull]this Editor editor)
        {
			return serializedObjectField.GetValue(editor) as SerializedObject;
        }

		[CanBeNull]
		internal static bool HasVisibleProperties([NotNull]this Editor editor)
		{
			var serializedObject = editor.GetSerializedObject();
			if(serializedObject == null)
            {
				return false;
            }
			var property = serializedObject.GetIterator();

            // Don't count script reference field property (since that is hidden in most editors)?
			return property.NextVisible(true) && property.NextVisible(false);
		}

		internal static float GetUnfoldedHeight([NotNull]this Editor editor)
        {
            var serializedObject = editor.GetSerializedObject();
            if(serializedObject == null)
            {
                return 0f;
            }

            SerializedProperty property = serializedObject.GetIterator();
            if(!property.NextVisible(true))
            {
                return 0f;
            }

            float height = 0f;
            do
            {
                height += EditorGUI.GetPropertyHeight(property, null, true);
            }
            while(property.NextVisible(property.isExpanded));

            return height;
        }

		internal static bool CanBeExpandedViaAFoldout([NotNull]this Editor editor)
        {
            return (bool)canBeExpandedViaAFoldout.Invoke(editor, null);
        }

        internal static void SetIsFirstInspectedEditor([NotNull]this Editor editor, bool value)
        {
            if(firstInspectedEditor == null)
            {
                #if DEV_MODE
				UnityEngine.Debug.LogWarning(editor.GetType().Name + ".SetIsFirstInspectedEditor(" + value+ ") was called but failed to find internal property Editor.firstInspectedEditor.");
				#endif
				return;
            }

            firstInspectedEditor.SetValue(editor, value, null);
        }
    }
}