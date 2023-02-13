//#define DEBUG_MIDDLE_CLICK

#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus.Compatibility
{
	/// <summary> Drawer that add better support for Script Inspector 3 by FLIPBOOK GAMES. </summary>
	public class ScriptInspectorDrawer : CustomEditorTextAssetDrawer
	{
		private static bool hideHeader = true;
		private static bool hideHeaderDetermined = false;

		public static bool HideHeader
		{
			get
			{
				if(!hideHeaderDetermined)
				{
					hideHeaderDetermined = true;
					hideHeader = EditorPrefs.GetBool("PI.SI3.HideHeader", true);
				}
				return hideHeader;
			}

			set
			{
				if(!hideHeaderDetermined || hideHeader != value)
				{
					hideHeader = value;
					EditorPrefs.SetBool("PI.SI3.HideHeader", value);
				}
			}
		}

		/// <inheritdoc/>
		public override bool WantsSearchBoxDisabled
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		public override float HeaderHeight
		{
			get
			{
				return hideHeader ? 0f : base.HeaderHeight;
			}
		}

		/// <inheritdoc/>
		public override Rect RightClickArea
		{
			get
			{
				if(hideHeader)
				{
					var rect = lastDrawPosition;
					const float scriptInspectorToolbarHeight = 35f;
					rect.height = scriptInspectorToolbarHeight;
					return rect;
				}
				return base.RightClickArea;
			}
		}

		/// <inheritdoc/>
		protected override void Setup([NotNull] Object[] setTargets, [CanBeNull] Object[] setEditorTargets, [CanBeNull] Type setEditorType, [CanBeNull] IParentDrawer setParent, [NotNull] IInspector setInspector)
		{
			hideHeader = HideHeader;
			base.Setup(setTargets, setEditorTargets, setEditorType, setParent, setInspector);
		}

		/// <inheritdoc/>
		public override bool Draw(Rect position)
		{
			if(!hideHeader)
			{
				return base.Draw(position);
			}

			UnityObjectDrawerUtility.HeadlessMode = true;
			try
			{
				bool dirty = base.Draw(position);
				UnityObjectDrawerUtility.HeadlessMode = false;
				return dirty;
			}
			catch(Exception e)
			{
				UnityObjectDrawerUtility.HeadlessMode = false;
				if(ExitGUIUtility.ShouldRethrowException(e))
				{
					throw e;
				}
				#if DEV_MODE
				Debug.LogError(e);
				#endif
				return false;
			}
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			if(!hideHeader)
			{
				return base.DrawPrefix(position);
			}

			return false;
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(keys.DetectTextFieldReservedInput(inputEvent, TextFieldType.TextArea))
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("Ignoring input because using ScriptInspectorDrawer");
				#endif
				return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc cref="IDrawer.OnMiddleClick" />
		public override void OnMiddleClick(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_MIDDLE_CLICK
			Debug.Log(ToString()+".OnMiddleClick("+StringUtils.ToString(inputEvent)+ ") with RightClickAreaMouseovered=" + StringUtils.ToColorizedString(RightClickAreaMouseovered) + ", Target="+StringUtils.ToString(Target)+", MonoScript="+StringUtils.ToString(MonoScript));
			#endif

			if(!RightClickAreaMouseovered)
			{
				return;
			}

			DrawGUI.Use(inputEvent);

			var target = Target;
			if(target != null)
			{
				DrawGUI.Active.PingObject(target);
			}
		}

		/// <inheritdoc/>
		public override void AddItemsToOpeningViewMenu(ref Menu menu)
		{
			base.AddItemsToOpeningViewMenu(ref menu);

			menu.AddSeparatorIfNotRedundant();
			menu.Add("Show Header", ()=> HideHeader = !HideHeader, !HideHeader);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			menu.AddSeparatorIfNotRedundant();
			menu.Add("Show Header", () => HideHeader = !HideHeader, !HideHeader);

			base.BuildContextMenu(ref menu, extendedMenu);
		}

		/// <inheritdoc />
		protected override void DrawImportedObjectGUI() { }
	}
}
#endif