#if UNITY_EDITOR
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class CustomEditorTextAssetDrawer : CustomEditorAssetDrawer
	{
		private float viewportHeight;

		/// <inheritdoc/>
		public override float Height
		{
			get
			{
				return viewportHeight;
			}
		}

		/// <inheritdoc/>
		protected override Editor Editor
		{
			get
			{
				return DebugMode ? base.Editor : HeaderEditor;
			}
		}

		/// <inheritdoc/>
		public override bool PrefixResizingEnabledOverControl
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override MonoScript MonoScript
		{
			get
			{
				return Target as MonoScript;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new CustomEditorTextAssetDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			CustomEditorTextAssetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new CustomEditorTextAssetDrawer();
			}
			result.Setup(targets, null, null, parent, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			var monoScript = MonoScript;
			if(monoScript == null)
			{
				TextAssetUtility.GetHeaderSubtitle(ref subtitle, LocalPath);
			}
			else
			{
				TextAssetUtility.GetHeaderSubtitle(ref subtitle, monoScript);
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 150f;
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			if(MonoScript != null)
			{
				#if UNITY_EDITOR
				menu.AddSeparatorIfNotRedundant();
				#if UNITY_2018_3_OR_NEWER
				menu.Add("Script Execution Order", () => DrawGUI.ExecuteMenuItem("Edit/Project Settings..."));
				#else
				menu.Add("Script Execution Order", () => DrawGUI.ExecuteMenuItem("Edit/Project Settings/Script Execution Order"));
				#endif
				#endif
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
		
		/// <inheritdoc />
		protected override void OnLayoutEvent(Rect position)
		{
			viewportHeight = CalculateViewportHeight();
			base.OnLayoutEvent(position);
		}
		
		private float CalculateViewportHeight()
		{
			float height = inspector.State.WindowRect.height - inspector.ToolbarHeight - inspector.PreviewAreaHeight;

			if(height < 0f)
			{
				return 0f;
			}

			return height;
		}
	}
}
#endif