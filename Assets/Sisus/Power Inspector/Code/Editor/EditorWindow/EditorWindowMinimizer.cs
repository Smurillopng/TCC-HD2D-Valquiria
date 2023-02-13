//#define DEBUG_CAN_MINIMIZE
//#define DEBUG_MINIMIZE
//#define DEBUG_UNMINIMIZE

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class EditorWindowMinimizer
	{
		[SerializeField]
		private EditorWindow window;

		[SerializeField]
		private ISelectionManager selectionManager;

		[SerializeField]
		private bool minimized;

		[SerializeField]
		private Vector2 minimizedDimensions;

		[SerializeField]
		private Rect positionBeforeMinimize;

		[SerializeField]
		private Vector2 minSizeBeforeMinimize;

		[SerializeField]
		private Vector2 maxSizeBeforeMinimize;

		[SerializeField]
		private int splitViewIndexBeforeMinimize;

		[SerializeField]
		private float splitViewPositionBeforeMinimize;

		[SerializeField]
		private int[] splitViewSizesBeforeMinimize;

		[SerializeField]
		private bool autoMinimize = false;

		public bool AutoMinimize
		{
			get
			{
				return autoMinimize;
			}

			set
			{
				autoMinimize = value;
			}
		}

		public bool Minimized
		{
			get
			{
				return minimized;
			}
		}

		public EditorWindowMinimizer() { }

		/// <summary> This should be called from OnEnable of the EditorWindow during each Layout event. </summary>
		public EditorWindowMinimizer([NotNull]EditorWindow targetWindow, [CanBeNull]ISelectionManager windowSelectionManager, bool doAutoMinimize)
		{
			Setup(targetWindow, windowSelectionManager, doAutoMinimize);
		}

		public void Setup([NotNull]EditorWindow targetWindow, [CanBeNull]ISelectionManager windowSelectionManager, bool doAutoMinimize)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(targetWindow != null);
			#endif

			window = targetWindow;

			selectionManager = windowSelectionManager;
			if(selectionManager != null)
			{
				autoMinimize = doAutoMinimize;
				selectionManager.OnSelectionChanged -= OnSelectionChanged;
				selectionManager.OnSelectionChanged += OnSelectionChanged;
			}
			else
			{
				autoMinimize = false;
			}
		}

		private void OnSelectionChanged()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(selectionManager != null);
			#endif
			
			if(!autoMinimize)
			{
				return;
			}

			if(selectionManager != null)
			{
				if(selectionManager.Selected.Length == 0)
				{
					Minimize();
				}
				else
				{
					Unminimize();
				}
			}
		}

		/// <summary> This should be called from OnGUI of the EditorWindow during each Layout event. </summary>
		public void OnLayout()
		{
			// if was minimized, but user manually resized dimensions, then unminimize
			if(minimized)
			{
				//for some reason when dragging Dock Area, this difference becomes 12. Otherwise it seems to be at 3.
				if(window.position.height - minimizedDimensions.y >= 13f) 
				{
					#if DEV_MODE
					Debug.Log("Unminimizing with position="+window.position.size+", minimizedDimensions="+minimizedDimensions+", diffX="+(window.position.width - minimizedDimensions.x)+", diffY="+(window.position.height - minimizedDimensions.y));
					#endif
					Unminimize();
				}
			}
		}

		/// <summary>
		/// Determine if EditorWindow minimized state can be changed at this time.
		/// 
		/// Currently minimizing is only supported if EditorWindow is not docked, or it is docked
		/// in a split view with another window in such a way that the minimized window resides
		/// below the other window. 
		/// </summary>
		/// <returns> True if window can be minimized or unminimized at this time, false if not. </returns>
		public bool CanMinimize()
		{
			var dockArea = window.ParentHostView();
			if(dockArea == null)
			{
				#if DEV_MODE && DEBUG_CAN_MINIMIZE
				Debug.Log("CanMinimize: True - because dockArea was null");
				#endif
				return true;
			}

			var parentProperty = dockArea.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);
			if(parentProperty == null)
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: DockArea property parent not found.");
				#endif
				return false;
			}
			var splitView = parentProperty.GetValue(dockArea, null);

			// allow minimizing if not in split view? Is this possible for modal windows?
			if(splitView == null)
			{
				#if DEV_MODE && DEBUG_CAN_MINIMIZE
				Debug.Log("CanMinimize: False - because SplitView was null");
				#endif
				return false;
			}

			var splitStateField = splitView.GetType().GetField("splitState", BindingFlags.Instance | BindingFlags.NonPublic);
			if(splitStateField == null)
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: splitState field not found.");
				#endif
				return false;
			}
			var splitState = splitStateField.GetValue(splitView);
			if(splitState == null)
			{
				var setupSplitter = splitView.GetType().GetMethod("SetupSplitter", BindingFlags.Instance | BindingFlags.NonPublic);
				if(setupSplitter == null)
				{
					#if DEV_MODE
					Debug.LogError("CanMinimize: SetupSplitter method not found.");
					#endif
					return false;
				}
				setupSplitter.Invoke(splitView);
				splitState = splitStateField.GetValue(splitView);
				if(splitState == null)
				{
					#if DEV_MODE
					Debug.LogError("CanMinimize: splitState was null event after calling SetupSplitter.");
					#endif
					return false;
				}
			}
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(splitState != null);
			#endif
			
			var verticalField = splitView.GetType().GetField("vertical", BindingFlags.Instance | BindingFlags.Public);
			if(verticalField == null)
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: verticalField field not found.");
				#endif
				return false;
			}
			bool vertical = (bool)verticalField.GetValue(splitView);
			
			// minimizing only supported for now if split is vertical
			if(!vertical)
			{
				#if DEV_MODE && DEBUG_CAN_MINIMIZE
				Debug.Log("CanMinimize: False - because !vertical");
				#endif
				return false;
			}

			var realSizesField = splitState.GetType().GetField("realSizes", BindingFlags.Instance | BindingFlags.Public);
			if(realSizesField == null)
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: realSizes field not found.");
				#endif
				return false;
			}

			float lastElementPosition = 0f;

			if(realSizesField.FieldType == typeof(float[]))
			{
				var realSizes = (float[])realSizesField.GetValue(splitState);
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(realSizes != null);
				#endif
			
				// if only window in dock area, allow minimizing
				if(realSizes.Length == 1)
				{
					#if DEV_MODE && DEBUG_CAN_MINIMIZE
					Debug.Log("CanMinimize: True - because realSizes.Length was 1");
					#endif
					return true;
				}

				for(int n = realSizes.Length - 2; n >= 0; n--)
				{
					lastElementPosition += realSizes[n];
				}
			}
			else if(realSizesField.FieldType == typeof(int[]))
			{
				var realSizes = (int[])realSizesField.GetValue(splitState);
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(realSizes != null);
				#endif
			
				// if only window in dock area, allow minimizing
				if(realSizes.Length == 1)
				{
					#if DEV_MODE && DEBUG_CAN_MINIMIZE
					Debug.Log("CanMinimize: True - because realSizes.Length was 1");
					#endif
					return true;
				}

				for(int n = realSizes.Length - 2; n >= 0; n--)
				{
					lastElementPosition += realSizes[n];
				}
			}
			else
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: realSizes field type " + realSizesField.FieldType.FullName + " not expected type.");
				#endif
				return false;
			}

			var screenPositionProperty = splitView.GetType().GetProperty("screenPosition", BindingFlags.Instance | BindingFlags.Public);
			if(screenPositionProperty == null)
			{
				#if DEV_MODE
				Debug.LogError("CanMinimize: screenPositionProperty property not found.");
				#endif
				return false;
			}

			var parentPosition = ((Rect)screenPositionProperty.GetValue(splitView, null)).y;
			float positionRelativeToParent = window.position.y - parentPosition;
			float posDiff = Mathf.Abs(positionRelativeToParent - lastElementPosition);

			#if DEV_MODE && DEBUG_CAN_MINIMIZE
			Debug.Log("lastElementPosition="+lastElementPosition+", position.y="+window.position.y+", parentPosition="+parentPosition+", positionRelativeToParent="+positionRelativeToParent+", posDiff="+posDiff+", realSizes[last]="+realSizes[realSizes.Length - 1]+", realSizes[0]="+realSizes[0]+", window.position.height="+window.position.height);
			#endif

			// minimizing only supported for now if window is last pane in split view
			if(posDiff > 1f)
			{
				#if DEV_MODE && DEBUG_CAN_MINIMIZE
				Debug.Log("CanMinimize: False - because window was not bottom element in split view");
				#endif
				return false;
			}
			
			#if DEV_MODE && DEBUG_CAN_MINIMIZE
			Debug.Log("CanMinimize: True");
			#endif

			return true;
		}

		/// <summary>
		/// Minimized the target window. If window is already minimized, nothing happens.
		/// 
		/// Currently minimizing is only supported if EditorWindow is not docked, or it is docked
		/// in a split view with another window in such a way that the minimized window resides
		/// below the other window. 
		/// </summary>
		public void Minimize()
		{
			if(minimized)
			{
				return;
			}
			
			positionBeforeMinimize = window.position;
			minSizeBeforeMinimize = window.minSize;
			maxSizeBeforeMinimize = window.maxSize;
			splitViewIndexBeforeMinimize = -1;

			var dockArea = window.ParentHostView();
			
			if(dockArea == null)
			{
				var setPosition = window.position;
				setPosition.height = 11f;
				window.position = setPosition;
				window.minSize = new Vector2(window.minSize.x, 11f);
				window.maxSize = new Vector2(window.maxSize.x, 11f);
				minimized = true;
			}
			else
			{
				var parentProperty = dockArea.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);
				if(parentProperty == null)
				{
					#if DEV_MODE
					Debug.LogError("Minimize: parent property not found.");
					#endif
					return;
				}
				var splitView = parentProperty.GetValue(dockArea, null);
				if(splitView != null && string.Equals(splitView.GetType().Name, "SplitView"))
				{
					var splitStateField = splitView.GetType().GetField("splitState", BindingFlags.Instance | BindingFlags.NonPublic);
					if(splitStateField == null)
					{
						#if DEV_MODE
						Debug.LogError("Minimize: splitState field not found.");
						#endif
						return;
					}
					var splitState = splitStateField.GetValue(splitView);
					if(splitState == null)
					{
						var setupSplitterMethod = splitView.GetType().GetMethod("SetupSplitter", BindingFlags.Instance | BindingFlags.NonPublic);
						if(setupSplitterMethod == null)
						{
							#if DEV_MODE
							Debug.LogError("Minimize: SetupSplitter method not found.");
							#endif
							return;
						}
						setupSplitterMethod.Invoke(splitView);

						splitState = splitStateField.GetValue(splitView);
						if(splitState == null)
						{
							#if DEV_MODE
							Debug.LogError("Minimize: splitState was null event after calling SetupSplitter.");
							#endif
							return;
						}
					}
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(splitState != null);
					#endif
					
					// minimizing in horizontal split not supported yet
					var verticalField = splitView.GetType().GetField("vertical", BindingFlags.Instance | BindingFlags.Public);
					if(verticalField == null)
					{
						#if DEV_MODE
						Debug.LogError("Minimize: vertical field not found.");
						#endif
						return;
					}
					bool vertical = (bool)verticalField.GetValue(splitView);
					if(!vertical)
					{
						#if DEV_MODE && DEBUG_MINIMIZE
						Debug.Log("aborting minimize because horizontal splits are not supported");
						#endif
						return;
					}

					var realSizesField = splitState.GetType().GetField("realSizes", BindingFlags.Instance | BindingFlags.Public);
					if(realSizesField == null)
					{
						#if DEV_MODE
						Debug.LogError("Minimize: realSizesField field not found.");
						#endif
						return;
					}

					if(realSizesField.FieldType != typeof(int[]))
					{
						if(realSizesField.FieldType != typeof(float[]))
						{
							#if DEV_MODE
							Debug.LogError("Minimize: realSizes field type " + realSizesField.FieldType.FullName + " not expected type.");
							#endif
							return;
						}

						var realSizes = (float[])realSizesField.GetValue(splitState);
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(realSizes != null);
						#endif

						if(realSizes.Length == 1)
						{
							#if DEV_MODE && DEBUG_MINIMIZE
							Debug.Log("minimizing with realSizes count 1");
							#endif

							var setPosition = window.position;
							setPosition.height = 11f;
							window.position = setPosition;
							window.minSize = new Vector2(window.minSize.x, 11f);
							window.maxSize = new Vector2(window.maxSize.x, 11f);
							minimized = true;
							minimizedDimensions = window.position.size;
							return;
						}

						if(splitViewSizesBeforeMinimize != null)
						{
							ArrayPool<int>.Dispose(ref splitViewSizesBeforeMinimize);
						}
						splitViewSizesBeforeMinimize = ArrayPool<int>.Create(realSizes.Length);
						Array.Copy(realSizes, splitViewSizesBeforeMinimize, realSizes.Length);

						splitViewIndexBeforeMinimize = realSizes.Length - 1;
						splitViewPositionBeforeMinimize = 0f;
						for(int n = 0; n < splitViewIndexBeforeMinimize; n++)
						{
							splitViewPositionBeforeMinimize += realSizes[n];
						}
					}
					else
					{
						var realSizes = (int[])realSizesField.GetValue(splitState);
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(realSizes != null);
						#endif

						if(realSizes.Length == 1)
						{
							#if DEV_MODE && DEBUG_MINIMIZE
							Debug.Log("minimizing with realSizes count 1");
							#endif

							var setPosition = window.position;
							setPosition.height = 11f;
							window.position = setPosition;
							window.minSize = new Vector2(window.minSize.x, 11f);
							window.maxSize = new Vector2(window.maxSize.x, 11f);
							minimized = true;
							minimizedDimensions = window.position.size;
							return;
						}

						if(splitViewSizesBeforeMinimize != null)
						{
							ArrayPool<int>.Dispose(ref splitViewSizesBeforeMinimize);
						}
						splitViewSizesBeforeMinimize = ArrayPool<int>.Create(realSizes.Length);
						Array.Copy(realSizes, splitViewSizesBeforeMinimize, realSizes.Length);

						splitViewIndexBeforeMinimize = realSizes.Length - 1;
						splitViewPositionBeforeMinimize = 0f;
						for(int n = 0; n < splitViewIndexBeforeMinimize; n++)
						{
							splitViewPositionBeforeMinimize += realSizes[n];
						}
					}

					#if DEV_MODE && DEBUG_MINIMIZE
					Debug.Log("splitViewIndexWas="+splitViewIndexBeforeMinimize+", sizesBeforeMinimize="+StringUtils.ToString(splitViewSizesBeforeMinimize)+", splitViewPositionWas="+splitViewPositionBeforeMinimize);
					#endif

					var placeView = splitView.GetType().GetMethod("PlaceView", BindingFlags.Instance | BindingFlags.NonPublic);
					if(placeView == null)
					{
						#if DEV_MODE
						Debug.LogError("Minimize: PlaceView method not found.");
						#endif
						return;
					}

					const float SetSize = 11f; //vertical ? 11f : 80f;
					float sizeChanged = splitViewSizesBeforeMinimize[splitViewIndexBeforeMinimize] - SetSize;
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(sizeChanged > 0f);
					#endif
					var parameters = new object[]{splitViewIndexBeforeMinimize, splitViewPositionBeforeMinimize + sizeChanged, SetSize};
					placeView.Invoke(splitView, parameters);
					
					var reflowMethod = splitView.GetType().GetMethod("Reflow", BindingFlags.Instance | BindingFlags.NonPublic);
					if(reflowMethod == null)
					{
						#if DEV_MODE
						Debug.LogError("Minimize: Reflow method not found.");
						#endif
						return;
					}
					reflowMethod.Invoke(splitView);

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(vertical);
					#endif

					window.minSize = new Vector2(window.minSize.x, SetSize);
					window.maxSize = new Vector2(window.maxSize.x, SetSize);

					minimized = true;
				}
				else
				{
					#if DEV_MODE && DEBUG_MINIMIZE
					Debug.Log("dockArea.parent: "+(splitView == null ? "null" : splitView.GetType().Name));
					Debug.Log("aborting minimize because splitView was null, with dockArea.parent="+(splitView == null ? "null" : splitView.GetType().Name));
					#endif
				}
			}
			
			minimizedDimensions = window.position.size;
		}

		/// <summary>
		/// Unminimized the target window. If window is not currently minimized, nothing happens.
		/// </summary>
		public void Unminimize()
		{
			if(!minimized)
			{
				return;
			}
			minimized = false;
			
			if(splitViewIndexBeforeMinimize != -1)
			{
				var dockArea = window.ParentHostView();
				if(dockArea != null)
				{
					var parentProperty = dockArea.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);
					if(parentProperty == null)
					{
						#if DEV_MODE && DEBUG_UNMINIMIZE
						Debug.LogError("Unminimize: parent property not found.");
						#endif
						return;
					}
					var splitView = parentProperty.GetValue(dockArea, null);
					if(splitView != null && splitView.GetType().Name == "SplitView")
					{
						var splitStateField = splitView.GetType().GetField("splitState", BindingFlags.Instance | BindingFlags.NonPublic);
						if(splitStateField == null)
						{
							#if DEV_MODE
							Debug.LogError("Unminimize: splitState field not found.");
							#endif
							return;
						}
						var splitState = splitStateField.GetValue(splitView);
						if(splitState == null)
						{
							var setupSplitter = splitView.GetType().GetMethod("SetupSplitter", BindingFlags.Instance | BindingFlags.NonPublic);
							if(setupSplitter == null)
							{
								#if DEV_MODE
								Debug.LogError("Unminimize: SetupSplitter method not found.");
								#endif
								return;
							}
							setupSplitter.Invoke(splitView);
							splitState = splitStateField.GetValue(splitView);
							if(splitState == null)
							{
								#if DEV_MODE
								Debug.LogError("Unminimize: splitState was null event after calling SetupSplitter.");
								#endif
								return;
							}
						}
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(splitState != null);
						#endif
					
						window.maxSize = maxSizeBeforeMinimize;
						window.minSize = minSizeBeforeMinimize;
						
						var placeView = splitView.GetType().GetMethod("PlaceView", BindingFlags.Instance | BindingFlags.NonPublic);
						if(placeView == null)
						{
							#if DEV_MODE
							Debug.LogError("Unminimize: PlaceView method not found.");
							#endif
							return;
						}

						var setSize = splitViewSizesBeforeMinimize[splitViewIndexBeforeMinimize];

						var parameters = new object[]{splitViewIndexBeforeMinimize, splitViewPositionBeforeMinimize, setSize};
						placeView.Invoke(splitView, parameters);

						var realSizesField = splitState.GetType().GetField("realSizes", BindingFlags.Instance | BindingFlags.Public);
						if(realSizesField == null)
						{
							#if DEV_MODE
							Debug.LogError("Unminimize: realSizes field not found.");
							#endif
							return;
						}

						realSizesField.SetValue(splitState, splitViewSizesBeforeMinimize);
						splitViewSizesBeforeMinimize = null;

						var reflowMethod = splitView.GetType().GetMethod("Reflow", BindingFlags.Instance | BindingFlags.NonPublic);
						if(reflowMethod == null)
						{
							#if DEV_MODE
							Debug.LogError("Minimize: Reflow method not found.");
							#endif
							return;
						}
						reflowMethod.Invoke(splitView);
						window.Focus();
						return;
					}
				}
			}

			window.maxSize = maxSizeBeforeMinimize;
			window.minSize = minSizeBeforeMinimize;

			var setPosition = window.position;
			setPosition.height = positionBeforeMinimize.height;
			window.position = setPosition;
		}

		/// <summary> Adds Minimize / Unminimize items to opening EditorWindow context menu. </summary>
		/// <param name="menu"> The opening EditorWindow context menu. </param>
		public void AddMinimizeItemsToMenu(GenericMenu menu)
		{
			bool canMinimize = CanMinimize();

			if(!minimized && !canMinimize)
			{
				menu.AddDisabledItem(new GUIContent("Minimize"));
			}
			else if(minimized)
			{
				menu.AddItem(new GUIContent("Minimize"), true, Unminimize);
			}
			else
			{
				menu.AddItem(new GUIContent("Minimize"), false, Minimize);
			}

			if(canMinimize)
			{
				menu.AddItem(new GUIContent("Auto-Minimize"), AutoMinimize, ToggleAutoMinimize);
			}
		}

		private void ToggleAutoMinimize()
		{
			AutoMinimize = !AutoMinimize;
		}
	}
}