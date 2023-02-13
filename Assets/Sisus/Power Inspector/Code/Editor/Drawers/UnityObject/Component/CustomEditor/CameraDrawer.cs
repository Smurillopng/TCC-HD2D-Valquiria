#if UNITY_EDITOR
using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Handles drawing the Camera component inside the inspector view.
	/// </summary>
	[Serializable, DrawerForComponent(typeof(Camera), false, true)]
	public class CameraDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		protected override int AppendLastCheckedId
		{
			get
			{
				return 200;
			}
		}

		/// <inheritdoc />
		protected override float EstimatedUnfoldedHeight
		{
			get
			{
				float lastHeight;
				if(lastUnfoldedHeightsByType.TryGetValue(typeof(Camera), out lastHeight))
                {
					return lastHeight;
				}
				return 380f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			#if UNITY_2019_1_OR_NEWER
			return 125f;
			#else
			return 73f;
			#endif
		}

		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			menu.AddSeparatorIfNotRedundant();
			menu.Add("Align With Scene View", () => DrawGUI.ExecuteMenuItem("GameObject/Align With View"));

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		#if UNITY_EDITOR
		private void AlignWithView()
		{
			var sceneViews = Resources.FindObjectsOfTypeAll<UnityEditor.SceneView>();
			if(sceneViews.Length > 0)
			{
				var sceneView = sceneViews[0];
				var gameObject = GameObject;
				if(inspector.IsSelected(gameObject))
				{
					sceneView.AlignWithView();
				}
				else
				{
					inspector.InspectorDrawer.SelectionManager.OnNextSelectionChanged(AlignWithView);
					inspector.Select(gameObject);
				}
			}
		}

		private void AlignWithView(UnityEngine.Object[] targets)
		{
			var sceneViews = Resources.FindObjectsOfTypeAll<UnityEditor.SceneView>();
			if(sceneViews.Length > 0)
			{
				var sceneView = sceneViews[0];
				sceneView.AlignWithView();
			}
		}

		#endif
	}
}
#endif