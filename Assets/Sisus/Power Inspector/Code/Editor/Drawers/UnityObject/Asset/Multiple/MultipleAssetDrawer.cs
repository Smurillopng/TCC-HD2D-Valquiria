#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Drawer for repesenting multiple assets of different types.
	/// </summary>
	[Serializable]
	public class MultipleAssetDrawer : CustomEditorAssetDrawer
	{
		private readonly TargetsGroupedByType targetsGrouped = new TargetsGroupedByType();

		/// <inheritdoc />
		public override PrefixResizer PrefixResizer
		{
			get
			{
				return PrefixResizer.Disabled;
			}
		}

		/// <inheritdoc />
		protected override bool HasDebugModeIcon
		{
			get
			{
				return false;
			}
		}
		
		#if UNITY_2018_1_OR_NEWER
		/// <inheritdoc />
		protected override bool HasPresetIcon
		{
			get
			{
				return false;
			}
		}
		#endif

		/// <inheritdoc />
		protected override bool HasReferenceIcon
		{
			get
			{
				return false;
			}
		}

        public override float HeaderHeight
        {
            get
            {
                return base.HeaderHeight + 4f;
            }
        }


        /// <inheritdoc />
        public override float Height
		{
			get
			{
				return HeaderHeight;
			}
		}

		/// <inheritdoc />
		protected override bool UsesEditorForDrawingBody
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		protected override bool DrawGreyedOut
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawer represent. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static new MultipleAssetDrawer Create(Object[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			MultipleAssetDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MultipleAssetDrawer();
			}
			result.Setup(targets, targets, null, parent, inspector);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc/>
		protected override void Setup(Object[] setTargets, Object[] setEditorTargets, Type setEditorType, IParentDrawer setParent, IInspector setInspector)
		{
			#if DEV_MODE
			Debug.Assert(setTargets.Length >= 2);
			#endif
			
			targetsGrouped.Setup(setTargets);
			
			#if DEV_MODE
			Debug.Assert(targetsGrouped.Count >= 2);
			#endif

			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
		}
		
		/// <inheritdoc />
		protected override void DoBuildHeaderButtons()
		{
			AddHeaderButton(Button.Create(InspectorLabels.Current.ShowInExplorer, ShowInExplorer));
		}

		/// <inheritdoc />
		protected override void DrawHeaderButtons()
		{
			HideInternalOpenButton();
			base.DrawHeaderButtons();
		}

		/// <inheritdoc />
		protected override void Open()
		{
			ShowInExplorer();
		}

		/// <inheritdoc/>
		protected override void DrawHeaderBase(Rect position)
		{
			DrawGUI.Active.AssetHeader(position, null, Label);
		}

		/// <inheritdoc/>
		public override bool DrawBody(Rect position)
		{
			position.y += 10f;
			position.height = DrawGUI.SingleLineHeight;
			position.x += DrawGUI.LeftPadding;
			position.width -= DrawGUI.LeftPadding + DrawGUI.RightPadding;

			var buttonRect = position;

			if(GUI.Button(buttonRect, "Switch To Stacked Mode"))
			{
				UserSettings.MergedMultiEditMode = false;
				Inspector.ForceRebuildDrawers();
			}

			position.y += position.height + 10f;

			GUI.Label(position, "Narrow the Selection:");
			position.y += position.height + 2f;

			bool changed = false;

			var backgroundRect = position;
			backgroundRect.width -= 19f;
			backgroundRect.height = DrawGUI.SingleLineHeight + 2f;

			var iconRect = backgroundRect;
			iconRect.width = 16f;
			iconRect.height = 16f;
			iconRect.x += 2f;
			iconRect.y += 1f;

			var labelRect = iconRect;
			labelRect.x += iconRect.width + 2f;
			labelRect.width = backgroundRect.width - iconRect.width - 4f;

			var peekRect = backgroundRect;
			peekRect.x = backgroundRect.xMax + 3f;
			peekRect.width = 20f;
			peekRect.height = 20f;

			float offset = DrawGUI.SingleLineHeight + 5f;

			for(int n = 0, count = targetsGrouped.Count; n < count; n++)
			{
				if(GUI.Button(backgroundRect, GUIContent.none, InspectorPreferences.Styles.MethodBackground))
				{
					if(Event.current.button == 0)
					{
						Inspector.Select(targetsGrouped[n].targets.ToArray());
					}
					else if(Event.current.button == 2)
					{
						DrawGUI.Ping(targetsGrouped[n].targets.ToArray());
					}

                    DrawGUI.Use(Event.current);
					changed = true;
				}
				else if(GUIUtility.hotControl == 0)
				{
                    DrawGUI.Active.AddCursorRect(backgroundRect, MouseCursor.Link);
				}

				var group = targetsGrouped[n];

				GUI.DrawTexture(iconRect, group.preview);

				GUI.Label(labelRect, group.label);
				
				GUIContent peekLabel = InspectorUtility.Preferences.labels.SplitViewIcon;
				var tooltipWas = peekLabel.tooltip;
				peekLabel.tooltip = "Peek";
				if(GUI.Button(peekRect, InspectorUtility.Preferences.labels.SplitViewIcon, InspectorPreferences.Styles.Centered))
				{
					var splittable = Inspector.InspectorDrawer as ISplittableInspectorDrawer;
					if(splittable != null && splittable.CanSplitView)
					{
						splittable.ShowInSplitView(targetsGrouped[n].targets.ToArray());
					}
					else
					{
						Inspector.Select(targetsGrouped[n].targets.ToArray());
					}
					DrawGUI.Use(Event.current);
					changed = true;
				}
				else if(GUIUtility.hotControl == 0)
				{
                    DrawGUI.Active.AddCursorRect(peekRect, MouseCursor.Link);
				}
				peekLabel.tooltip = tooltipWas;

				backgroundRect.y += offset;
				labelRect.y += offset;
				iconRect.y += offset;
				peekRect.y += offset;
			}

			return changed;
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers() { }

		/// <inheritdoc />
		protected override void GetHeaderSubtitle(ref GUIContent subtitle)
		{
			subtitle.text = StringUtils.Concat(targetsGrouped.Count, " Types Of Assets");
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			targetsGrouped.Clear();
			base.Dispose();
		}
	}
}
#endif