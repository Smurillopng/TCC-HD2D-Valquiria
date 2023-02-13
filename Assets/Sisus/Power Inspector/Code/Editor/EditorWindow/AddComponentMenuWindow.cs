#define REPAINT_ON_UPDATE

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Window for viewing the add component menu in the editor.
	/// This class cannot be inherited.
	/// </summary>
	public sealed class AddComponentMenuWindow : EditorWindow
	{
		private static AddComponentMenuWindow instance;

		//important that this is static, since it needs to be referenced after OnDestroy
		private static IInspector inspector;

		private AddComponentMenuDrawer drawer;
		private Action onClosed;
		private int isDirty;

		public static bool IsOpen
		{
			get
			{
				return instance != null;
			}
		}

		/// <summary>
		/// Creates the Add Component Menu Window if in editor mode.
		/// This is attached to the Add Component button being clicked.
		/// </summary>
		/// <param name="inspector"> The inspector which contains the target. </param>
		/// <param name="target"> Target onto which components should be added. </param>
		/// <param name="unrollPosition"> The position above or below which the window should open. </param>
		/// <param name="onClosed"> This action is invoked when the editor window is closed. </param>
		public static void CreateIfInEditorMode(IInspector inspector, IGameObjectDrawer target, Rect unrollPosition, Action onClosed)
		{
			if(Platform.EditorMode)
			{
				Create(inspector, target, unrollPosition, onClosed);
			}
		}

		/// <summary>
		/// Creates the Add Component Menu Window if it doesn't already exist.
		/// </summary>
		/// <param name="inspector">   The inspector which contains the target. </param>
		/// <param name="target">	   Target onto which components should be added. </param>
		/// <param name="unrollPosition">The position above or below which the window should open. </param>
		/// <param name="onClosed">	   This action is invoked when the editor window is closed. </param>
		public static void Create(IInspector inspector, IGameObjectDrawer target, Rect unrollPosition, Action onClosed)
		{
			if(instance != null)
			{
				return;
			}
			instance = CreateInstance<AddComponentMenuWindow>();

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(instance.IsVisible());
			#endif

			try
			{
				instance.Setup(inspector, target, unrollPosition, onClosed);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch
			{
			#endif
				if(instance != null)
				{
					Destroy(instance);
				}
				RestoreClickControls();
			}
		}

		/// <summary>
		/// Setup the newly created instance.
		/// </summary>
		/// <param name="setInspector">  The inspector which contains the target. </param>
		/// <param name="target"> Target onto which components should be added Target onto which components should be added. </param>
		/// <param name="unrollPosition">The button position. </param>
		/// <param name="onClosed">
		/// This action is invoked
		/// when the editor window is
		/// closed. </param>
		private void Setup(IInspector setInspector, IGameObjectDrawer target, Rect unrollPosition, Action onClosed)
		{
			inspector = setInspector;
			var inspectorDrawer = inspector.InspectorDrawer;
			this.onClosed = onClosed;
			
			inspector.InspectorDrawer.Manager.IgnoreAllMouseInputs = true;

			var unrollPosScreenSpace = unrollPosition;

			//menu should open underneath the button
			unrollPosScreenSpace.y += unrollPosition.height - 1f;

			// add inspector y-axis position to open position
			// so that if this is a split view, the menu is opened
			// with the correct offset
			unrollPosScreenSpace.y += inspector.State.WindowRect.y;

			//TO DO: if it's off-screen, scroll to it!
			//if it's hidden via filter field, maybe clear the filter?
			//or maybe never hide it via the filter field?
			//or maybe disable the keyboard shortcut if it's hidden
			//even trigger it via the addcomponentmenu item?
			if(setInspector.IsOutsideViewport(unrollPosScreenSpace))
			{
				unrollPosScreenSpace.y = inspectorDrawer.position.y + inspectorDrawer.position.height - AddComponentMenuDrawer.TotalHeight;
			}
			else
			{
				unrollPosScreenSpace.y += inspectorDrawer.position.y - setInspector.State.ScrollPos.y + 20f;
			}

			var screenHeight = Screen.currentResolution.height;

			//if there's not enough screen estate to draw the menu below
			//the add component button then draw it above
			if(unrollPosScreenSpace.y + AddComponentMenuDrawer.TotalHeight > screenHeight)
			{
				unrollPosScreenSpace.y -= AddComponentMenuDrawer.TotalHeight + unrollPosition.height + 2f;
			}

			unrollPosScreenSpace.x = Mathf.CeilToInt(inspectorDrawer.position.x + setInspector.State.contentRect.width * 0.5f - AddComponentMenuDrawer.Width * 0.5f);

			// if the inspector window is docked, the positions need to be adjusted somewhat to be accurate
			var inspectorWindow = setInspector.InspectorDrawer as EditorWindow;
			if(inspectorWindow != null)
			{
				if(inspectorWindow.IsDocked())
				{
					unrollPosScreenSpace.x += 2f;
					unrollPosScreenSpace.y -= 4f;
				}
			}

			if(drawer == null)
			{
				drawer = AddComponentMenuDrawer.Create(inspector, target, unrollPosition, Close);
			}
			else
			{
				drawer.Setup(inspector, target, unrollPosition, Close);
			}
			
			ShowAsDropDown(unrollPosScreenSpace, new Vector2(AddComponentMenuDrawer.Width, AddComponentMenuDrawer.TotalHeight));
		}
		
		[UsedImplicitly]
		private void OnGUI()
		{
			GUI.depth = -100;

			//handle assembly reload causing drawer to go null, resulting in null reference exceptions
			if(drawer == null)
			{
				Close();
				return;
			}

			var preferences = inspector.Preferences;

			DrawGUI.BeginOnGUI(preferences, true);

			bool addedComponent = false;
			EditorGUI.BeginChangeCheck();
			{
				if(drawer.OnGUI(ref addedComponent))
				{
					GUI.changed = true;
					Repaint();
					isDirty = 3;
				}
			}
			if(EditorGUI.EndChangeCheck())
			{
				isDirty = 3;
			}

			if(addedComponent)
			{
				Close();
				return;
			}

			if(isDirty > 0)
			{
				GUI.changed = true;
				Repaint();
			}

			var rect = position;
			rect.x = 0f;
			rect.y = 0f;
			DrawGUI.DrawRect(rect, preferences.theme.ComponentSeparatorLine);
		}

		#if REPAINT_ON_UPDATE
		[UsedImplicitly]
		private void Update()
		{
			Repaint();
		}
		#else
		[UsedImplicitly]
		private void Update()
		{
			if(isDirty > 0)
			{
				isDirty--;
				GUI.changed = true;
				Repaint();
			}
		}
		#endif

		[UsedImplicitly]
		private void OnDestroy()
		{
			#if DEV_MODE
			Debug.Log("AddComponentMenuWindow.OnDestroy");
			#endif

			RestoreClickControlsAfterTwoFrames();
			Dispose();
		}

		private static void RestoreClickControlsAfterTwoFrames()
		{
			if(inspector != null)
			{
				inspector.OnNextLayout(RestoreClickControlsAfterAFrame);
			}
			#if DEV_MODE
			else { Debug.Log("RestoreClickControlsAfterTwoFrames called with inspector null, so can't restore controls. InspectorUtility.ActiveInspector="+StringUtils.ToString(InspectorUtility.ActiveInspector)); }
			#endif
		}

		private static void RestoreClickControlsAfterAFrame()
		{
			if(inspector != null)
			{
				inspector.OnNextLayout(RestoreClickControls);
			}
			#if DEV_MODE
			else { Debug.Log("RestoreClickControlsAfterAFrame called with inspector null, so can't restore controls. InspectorUtility.ActiveInspector=" + StringUtils.ToString(InspectorUtility.ActiveInspector)); }
			#endif
		}

		private static void RestoreClickControls()
		{
			if(inspector != null)
			{
				inspector.InspectorDrawer.Manager.IgnoreAllMouseInputs = false;
			}
			#if DEV_MODE
			else { Debug.Log("RestoreClickControls called with inspector null, so can't restore controls. InspectorUtility.ActiveInspector=" + StringUtils.ToString(InspectorUtility.ActiveInspector)); }
			#endif
		}

		private void Dispose()
		{
			if(instance != null)
			{
				instance = null;

				if(drawer != null)
				{
					drawer.OnClosed();
				}

				if(onClosed != null)
				{
					var callback = onClosed;
					onClosed = null;
					callback();
				}
			}
		}
	}
}