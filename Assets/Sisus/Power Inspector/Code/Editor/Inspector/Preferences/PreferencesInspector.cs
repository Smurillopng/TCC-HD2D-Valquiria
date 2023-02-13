#define SAFE_MODE

//#define DEBUG_SELECT
//#define DEBUG_CLICK
//#define DEBUG_MOUSEOVERED_PART

using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	[Serializable]
	public sealed class PreferencesInspector : Inspector<PowerInspectorPreferencesWindow, PreferencesToolbar>
	{
		private static InspectorPreferences preferencesCached;
		
		/// <inheritdoc />
		protected override PreviewDrawer PreviewDrawer
		{
			get
			{
				return null;
			}
		}
		
		/// <summary> Initializes the PreferencesInspector instance. </summary>
		/// <param name="inspector"> The inspector. </param>
		/// <param name="preferences"> inspector preferences. </param>
		/// <param name="drawer"> The drawer. </param>
		/// <param name="inspected"> The inspected targets. </param>
		/// <param name="scrollPos"> The viewport scroll position. </param>
		/// <param name="viewIsLocked"> True if view is locked. </param>
		public static void Setup(PreferencesInspector inspector, InspectorPreferences preferences, PowerInspectorPreferencesWindow drawer, Object[] inspected, Vector2 scrollPos, bool viewIsLocked)
		{
			inspector.Setup(drawer, preferences, inspected, scrollPos, viewIsLocked);
		}

		/// <inheritdoc/>
		public override void Setup(IInspectorDrawer drawer, InspectorPreferences setPreferences, Object[] inspected, Vector2 scrollPos, bool viewIsLocked)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setPreferences != null);
			#endif

			// Call base.Setup before calling Setup for the toolbar, so that the Preferences field gets assigned first
			base.Setup(drawer, setPreferences, inspected, scrollPos, viewIsLocked);
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Preferences != null);
			Debug.Assert(Preferences == setPreferences);
			#endif
		}

		/// <summary>
		/// Gets the preferences asset for 
		/// </summary>
		/// <returns> The preferences for  </returns>
		public static InspectorPreferences GetPreferences()
		{
			return InspectorPreferences.GetSettingsCached(ref preferencesCached, true);
		}

		/// <summary> This should be called by the IInspectorDrawer of the inspector during every OnGUI event. </summary>
		/// <param name="inspectorDimensions"> The position and bounds for where the inspecto should be drawn. </param>
		/// <param name="anyInspectorPartMouseovered"> True if any inspector part is currently mouseovered. </param>
		public override void OnGUI(Rect inspectorDimensions, bool anyInspectorPartMouseovered)
		{
			UnityEngine.Profiling.Profiler.BeginSample("OnGUI");
			
			#if DEV_MODE && DEBUG_CLICK
			var e = Event.current;
			if(e.rawType == EventType.MouseDown) { Debug.Log(StringUtils.ToColorizedString(ToString()+" Event=", e, ", e.type=", e.type, ", button=", e.button, ", mousePos=", e.mousePosition, ", GUIUtility.hotControl=", GUIUtility.hotControl)); }
			#endif

			//this can happen e.g. if the preferences file gets reimported due to being altered outside of Unity
			if(Preferences == null)
			{
				Preferences = GetPreferences();
			}

			#if DEV_MODE && DEBUG_MOUSEOVERED_PART
			if(State.drawer.VisibleMembers.Length > 0 && DrawGUI.IsUnityObjectDrag)
			{
				Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnGUI with mouseoveredPart=", MouseoveredPart, ", Event="+StringUtils.ToString(Event.current), ", ignoreAllMouseInputs=", InspectorDrawer.Manager.IgnoreAllMouseInputs, "´, ObjectPickerIsOpen=", ObjectPicker.IsOpen, ", anyInspectorPartMouseovered=", anyInspectorPartMouseovered, ", InspectorDrawer.MouseIsOver=", InspectorDrawer.MouseIsOver, ", DrawGUI.CanRequestMousePosition=", Cursor.CanRequestLocalPosition));
			}
			#endif

			InspectorUtility.BeginInspector(this, ref anyInspectorPartMouseovered);
			
			Rect toolbarRect;
			Rect viewportRect;
			Rect previewAreaRect;
			GetDrawPositions(inspectorDimensions, out toolbarRect, out viewportRect, out previewAreaRect);
			
			// trying to fix a bug where the default inspector layout gets wacky if both it and this window are open
			// by making sure all values that could affect it are restored back to normal
			// var indentLevelWas = EditorGUI.indentLevel;
			#if UNITY_EDITOR
			var labelWidthWas = EditorGUIUtility.labelWidth;
			#endif
			var matrixWas = GUI.matrix;
	
			var currentEvent = Event.current;
			switch(currentEvent.type)
			{
				case EventType.Layout:
					State.nextUpdateCachedValues--;
					if(State.nextUpdateCachedValues <= 0)
					{
						UpdateCachedValuesFromFields();
					}
					OnCursorPositionOrLayoutChanged();
					break;
                case EventType.MouseMove:
				case EventType.MouseDrag:
				case EventType.DragUpdated:
					if(IgnoreViewportMouseInputs())
					{
						#if DEV_MODE
						//Debug.Log("ignoring "+ currentEvent.type+"...");
						#endif
						break;
					}
					
					OnCursorPositionOrLayoutChanged();
					InspectorDrawer.RefreshView();
					break;
			}

			bool dirty;
			try
			{
				dirty = DrawViewport(viewportRect);
			}
			catch(Exception e)
			{
				if(ExitGUIUtility.ShouldRethrowException(e))
				{
					NowDrawingPart = InspectorPart.None;
					DrawGUI.IndentLevel = 0;
					#if UNITY_EDITOR
					EditorGUIUtility.labelWidth = labelWidthWas;
					#endif
					GUI.skin = null;
					GUI.matrix = matrixWas;
					throw;
				}
				#if DEV_MODE
				Debug.LogWarning(ToString()+" "+e);
				#endif
				dirty = true;
			}

			#if !POWER_INSPECTOR_LITE
			NowDrawingPart = InspectorPart.Toolbar;
			{
				Toolbar.Draw(toolbarRect);
			}
			#endif
			NowDrawingPart = InspectorPart.Other;
			
			//TO DO: Move to EndInspector if these are needed?
			//trying to fix a bug where the default inspector layout gets wacky if both it and this window are open
			//by making sure all values that could affect it are restored back to normal
			DrawGUI.IndentLevel = 0;
			#if UNITY_EDITOR
			EditorGUIUtility.labelWidth = labelWidthWas;
			#endif
			GUI.skin = null;
			GUI.matrix = matrixWas;
			
			if(dirty)
			{
				InspectorDrawer.RefreshView();
			}

			InspectorUtility.EndInspector(this);

			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// <inheritdoc/>
		public override void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			switch(messageType)
			{
				case MessageType.Error:
					Debug.LogError(message.text, context);
					return;
				case MessageType.Warning:
					Debug.LogWarning(message.text, context);
					return;
				default:
					Debug.Log(message.text, context);
					return;
			}
		}

		/// <inheritdoc/>
		public override void Select(Object target)
		{
			#if DEV_MODE && DEBUG_SELECT
			Debug.Log(ToString()+".Select("+StringUtils.ToString(target) +")");
			#endif

			#if UNITY_EDITOR
			Selection.activeObject = target;
			#else
			State.inspected = ArrayPool<Object>.CreateWithContent(target);
			#endif
		}

		/// <inheritdoc/>
		public override void Select(Object[] targets)
		{
			#if DEV_MODE && DEBUG_SELECT
			Debug.Log(ToString()+".Select("+StringUtils.ToString(targets)+")");
			#endif

			#if UNITY_EDITOR
			Selection.objects = targets;
			#else
			State.inspected = targets;
			#endif
		}
		
		public override string ToString()
		{
			if(InspectorDrawer != null)
			{
				if(InspectorDrawer.MainView == this)
				{
					return "PreferencesInspector";
				}
				var splittable = InspectorDrawer as ISplittableInspectorDrawer;
				if(splittable != null && splittable.SplitView == this)
				{
					return "PreferencesInspector/SplitView";
				}
			}
			return "PreferencesInspector(???)";
		}
		
		/// <inheritdoc/>
		protected override bool ShouldDrawPreviewArea()
		{
			return false;
		}
		
		/// <inheritdoc/>
		public override void OnProjectOrHierarchyChanged(OnChangedEventSubject changed, bool forceRebuildDrawers)
		{
			if(Preferences == null)
			{
				Preferences = GetPreferences();
			}
			UpdateDrawPreviewArea();

			base.OnProjectOrHierarchyChanged(changed, forceRebuildDrawers);
		}

		/// <inheritdoc/>
		protected override void SetupToolbar(IInspectorToolbar setupToolbar)
		{
			setupToolbar.Setup(this);
		}
	}
}