#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForComponent(typeof(RectTransform), false, true)]
	public class RectTransformDrawer : CustomEditorComponentDrawer
	{
		/// <inheritdoc />
		public override float MinPrefixLabelWidth
		{
			get
			{
				return 60f;
			}
		}

		/// <inheritdoc/>
		protected override float GetOptimalPrefixLabelWidthForEditor(int indentLevel)
		{
			return 72f;
		}

		/// <inheritdoc/>
		protected override float EstimatedUnfoldedHeight
        {
            get
            {
                return 259f;
            }
        }

        /// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
        /// <param name="targets"> The targets that the drawer represent. </param>
        /// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
        /// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
        /// <returns> The instance, ready to be used. </returns>
        public static RectTransformDrawer Create(Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			RectTransformDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new RectTransformDrawer();
			}
			result.Setup(targets, parent, inspector, Types.GetInternalEditorType("UnityEditor.RectTransformEditor"));
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void LateSetup()
		{
			base.LateSetup();
			OptimizePrefixLabelWidth();
		}

		/// <inheritdoc />
		protected override bool GetShouldSkipControl(Rect prevRect, Rect rect, Rect bounds, int id, out bool wasOutOfBounds)
		{
			if(rect.y < 72f)
			{
				if(!rect.height.Equals(32f))
				{
					wasOutOfBounds = false;

					#if DEV_MODE
					Debug.LogWarning("skipping control with id "+ id + " because y value "+rect.y+" was less than 72 but height "+rect.height+" was not 32");
					#endif
					return true;
				}
			}

			if((rect.y % 2f).Equals(0f))
			{
				wasOutOfBounds = false;

				#if DEV_MODE
				Debug.LogWarning("skipping control with id "+ id + " because y value "+rect.y+" is not valid");
				#endif
				return true;
			}
			
			return base.GetShouldSkipControl(prevRect, rect, bounds, id, out wasOutOfBounds);
		}

		/// <inheritdoc />
		public override void Duplicate()
		{
			for(int n = 0, count = targets.Length; n < count; n++)
			{
				var source = (RectTransform)targets[n];
				var go = new GameObject(source.name, typeof(RectTransform));
				var clone = go.GetComponent<RectTransform>();
				clone.position = source.position;
				clone.rotation = source.rotation;
				clone.localScale = source.localScale;
				clone.tag = source.tag;
				clone.gameObject.layer = source.gameObject.layer;
				clone.gameObject.SetActive(source.gameObject.activeSelf);
				clone.gameObject.isStatic = source.gameObject.isStatic;
			}
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(extendedMenu && !ReadOnly)
			{
				menu.Add("Convert to Transform", ConvertToTransform);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void ConvertToTransform()
		{
			var wasLocked = Inspector.State.ViewIsLocked;
			Inspector.State.ViewIsLocked = true;
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				Platform.Active.Destroy(targets[n]);
			}
			Inspector.State.ViewIsLocked = wasLocked;
		}
	}
}
#endif