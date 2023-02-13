using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class GameObjectHeaderDrawer
	{
		/// <summary>
		/// NOTE: Used by Hierarchy Folders - don't change the public API!
		/// </summary>
		public const float OpenInPrefabModeButtonHeight = 30f;

		private static readonly GUIContent OpenPrefabButtonLabel = new GUIContent("Open In Prefab Mode", "Open in Prefab Mode for full editing support.");
		
		[SerializeField]
		private GameObject[] targets;

		[SerializeField]
		private Editor editor;
		private bool isPrefab;
		
		public void SetTargets([NotNull]GameObject[] setTargets, [NotNull]Editors editorProvider)
		{
			targets = setTargets;

			editorProvider.GetEditorInternal(ref editor, targets, null, true);

			isPrefab = targets.Length > 0 && targets[0].IsPrefab();
		}

		public Rect Draw(Rect position)
		{
			bool headerHeightDetermined = true;
			var actualDrawnPosition = EditorGUIDrawer.AssetHeader(position, editor, ref headerHeightDetermined);

			if(!isPrefab)
			{
				return actualDrawnPosition;
			}

			const float padding = 3f;
			const float doublePadding = padding + padding;

			position.y += position.height - OpenInPrefabModeButtonHeight + padding;
			position.height = OpenInPrefabModeButtonHeight - doublePadding;

			DrawGUI.Active.ColorRect(position, InspectorUtility.Preferences.theme.AssetHeaderBackground);

			position.x += padding;
			position.width -= doublePadding;

			// UPDATE: even if prefab is being drawn in grayed out color
			// due to being inactive, draw the open prefab button without
			// being grayed out, to make it clear that it remains usable.
			var guiColorWas = GUI.color;
			var setColor = guiColorWas;
			setColor.a = 1f;
			GUI.color = setColor;
			if(DrawGUI.Active.Button(position, OpenPrefabButtonLabel))
			{
				DrawGUI.UseEvent();
				GameObjectDrawer.OpenPrefab(targets[0]);
			}

			GUI.color = guiColorWas;

			actualDrawnPosition.height += OpenInPrefabModeButtonHeight;

			return actualDrawnPosition;
		}

		public void ResetState()
		{
			targets = null;

			#if UNITY_EDITOR
			if(!ReferenceEquals(editor, null))
			{
				Editors.Dispose(ref editor);
			}
			#endif
		}

		public void OnProjectOrHierarchyChanged(GameObject[] setTargets, IInspector inspector)
		{
			targets = setTargets;

			#if UNITY_EDITOR
			if(editor == null || Editors.DisposeIfInvalid(ref editor))
			{
				inspector.InspectorDrawer.Editors.GetEditorInternal(ref editor, ArrayPool<GameObject>.Cast<Object>(targets), null, true);
			}
			#endif
		}
	}
}