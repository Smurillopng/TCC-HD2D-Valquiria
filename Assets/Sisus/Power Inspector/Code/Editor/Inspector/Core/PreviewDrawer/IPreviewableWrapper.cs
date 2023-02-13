using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public interface IPreviewableWrapper
	{
		bool StateIsValid { get; }

		PreviewableKey Key { get; }

		/// <summary> Gets the type of the wrapped IPreviewable. </summary>
		/// <value> Previewable type. </value>
		Type Type { get; }

		Object[] Targets { get; }

		bool HasPreviewGUI();

		string GetInfoString();
		GUIContent GetPreviewTitle();

		void OnPreviewSettings();
		void OnPreviewGUI(Rect position, GUIStyle background);
		void OnInteractivePreviewGUI(Rect position, GUIStyle background);
		
		void DrawPreview(Rect previewArea);

		void Dispose();

		void ReloadPreviewInstances();

		void OnForceReloadInspector();

		void OnBecameActive(Object[] targetObjects);

		void SetIsFirstInspectedEditor(bool value);

		bool RequiresConstantRepaint
		{
			get;
		}
	}
}